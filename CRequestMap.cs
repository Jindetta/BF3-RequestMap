/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Timers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Maps;
using PRoCon.Core.Plugin;

namespace PRoConEvents
{
    public class CRequestMap: PRoConPluginAPI, IPRoConPluginInterface
    {
    	#region Variables

    	// General variables
    	private int playerCount, cooldownDelay;
        private int approveThreshold, approvalCount;
        private string requestee;

        // Plugin variables
        private bool pluginEnabled;

        // Timer related
        private int mapSwitchDelay, voteDuration;
        private DateTime requestDelay, requestTime;
        private Timer delayTimer, durationTimer;

        // Map variables
        private int requestedMap, nextMap;
      	private List<MaplistEntry> currentMaps;

        #endregion
		#region General functionality

        private void resetVariables()
        {
        	requestedMap = -1;
        	approvalCount = 0;

        	requestee = "";
        	requestDelay = DateTime.MinValue;
        }

        public CRequestMap()
        {
            voteDuration = 60;
            mapSwitchDelay = 15;
            cooldownDelay = 30;
            approveThreshold = 1;

           	currentMaps = new List<MaplistEntry>();
            OnPluginDisable();
        }

		public void OnServerInfo( CServerInfo serverInfo )
		{
			playerCount = serverInfo.PlayerCount;
		}

        #endregion
        #region Plugin information

        public string GetPluginName()
        {
        	return "Request Map";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.0";
        }

        public string GetPluginAuthor()
        {
            return "JiN";
        }

        public string GetPluginWebsite()
        {
            return "http://battlelog.battlefield.com/bf3/user/Jindetta/";
        }

        public string GetPluginDescription()
        {
            return @"Very simple map request approval system to allow players to change map";
        }

        public void OnPluginLoaded( string hostName, string portNr, string PRoConVersion )
        {
			RegisterEvents( GetType().Name, "OnServerInfo", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnMaplistList", "OnMaplistGetMapIndices" );
			pluginLogConsole( "Procedure was executed!", "RegisterEvents()" );
        }

        public void OnPluginEnable()
        {
            pluginEnabled = true;
            pluginLogConsole( "Procedure was executed!", "PluginEnable()" );
        }

        public void OnPluginDisable()
        {
            pluginEnabled = false;
            pluginLogConsole( "Procedure was executed!", "PluginDisable()" );

 	       	resetVariables();
        }

        private void pluginLogConsole( string message, string key )
        {
        	switch( key.ToLower() )
        	{
        		case "error":
        			key = "^8" + key;
        		break;
        		case "notice":
        			key = "^2" + key;
        		break;
        		default:
        			key = "^4" + key;
        		break;
        	}

            message = "^b[" + GetPluginName() + "] " + key + "^0^n: " + message;
            ExecuteCommand( "procon.protected.pluginconsole.write", message );
        }

        #endregion
        #region Plugin variables

       	public List<CPluginVariable> GetDisplayPluginVariables()
       	{
            return new List<CPluginVariable>();
        }

        public List<CPluginVariable> GetPluginVariables()
        {
        	return GetDisplayPluginVariables();
        }

		public void SetPluginVariable( string varName, string varValue )
		{
		}

        #endregion
        #region Timer and events

        private void mapDelayEvent( object source, ElapsedEventArgs e )
        {
        	if( source is Timer )
        	{
        		( source as Timer ).Stop();
        		( source as Timer ).Dispose();

        		if( pluginEnabled && !requestDelay.Equals( DateTime.MinValue ) )
        		{
	      			setMap(
	            		currentMaps[nextMap].MapFileName,
						currentMaps[nextMap].Gamemode
					);
	
        			pluginLogConsole( "Loading requested map...", "Notice" );
	            	ExecuteCommand( "procon.protected.send", "mapList.runNextRound" );
        			resetVariables();
        		}

	            ExecuteCommand( "procon.protected.send", "mapList.getMapIndices" );
        	}
        }

        private void votingFinishedEvent( object source, ElapsedEventArgs e )
        {
        	if( source is Timer )
        	{
        		( source as Timer ).Stop();
        		( source as Timer ).Dispose();

        		if( !requestDelay.Equals( DateTime.MinValue ) )
        		{
	        		if( pluginEnabled && approvalCount >= approveThreshold )
	            	{
	            		nextMap = requestedMap;
	            		delayTimer = setTimer( mapDelayEvent, mapSwitchDelay );
	
	            		pluginLogConsole( "Requested map was approved...", "Notice" );
	            		notifyAllPlayers( "Next map will load in " + mapSwitchDelay + " seconds..." );
	            	}
	        		else
	        		{
	        			if( approvalCount < approveThreshold )
	        			{
	        				resetVariables();
	            			notifyAllPlayers( "Map request was not approved in time (" + approvalCount + "/" + approveThreshold + ")" );
	        			}
	        		}
        		}
        	}
        }

        private Timer setTimer( ElapsedEventHandler eventHandler, int seconds )
        {
        	Timer timer = new Timer( seconds * 1000 );

        	pluginLogConsole( "Procedure was executed!", "SetTimer()" );
        	timer.Elapsed += eventHandler;
        	timer.Start();

        	return timer;
        }

        #endregion
        #region Map information/manipulation

        private void setMap( string mapName, string gameMode )
        {
            for( int i = 0; i < currentMaps.Count; i++ )
            {
                if( currentMaps[i].MapFileName.Equals( mapName ) && currentMaps[i].Gamemode.Equals( gameMode ) )
                {
                   	ExecuteCommand( "procon.protected.send", "mapList.setNextMapIndex", i.ToString() );
                    break;
                }
            }
        }

        public void OnMaplistList( List<MaplistEntry> mapList )
        {
        	currentMaps = new List<MaplistEntry>( mapList );
        	pluginLogConsole( "Maplist was loaded (+" + currentMaps.Count + ")!", "Notice" );
        }

		public void OnMaplistGetMapIndices( int mapIndex, int nextIndex )
		{
			if( requestDelay.Equals( DateTime.MinValue ) || nextMap != requestedMap )
				nextMap = nextIndex;
		}

		#endregion
		#region Chat processing

		private void notifyAllPlayers( string message )
		{
			ExecuteCommand( "procon.protected.send", "admin.say", message, "all" );
		}

		private void notifyPlayer( string message, string player )
		{
			ExecuteCommand( "procon.protected.send", "admin.say", message, "player", player );
		}

		private string gameModeToShort( string gameMode )
		{
			switch( gameMode )
			{
                case "ConquestLarge0":			return "[CQ]";
                case "ConquestSmall0":			return "[CQ]";
                case "ConquestAssaultSmall0":	return "[CQA]";
                case "ConquestAssaultSmall1":	return "[CQA]";
                case "ConquestAssaultLarge0":	return "[CQA]";
                case "Domination0":				return "[CQDOM]";
                case "RushLarge0":				return "[RUSH]";
                case "SquadRush0":				return "[SQRUSH]";
                case "SquadDeathMatch0":		return "[SQDM]";
                case "TeamDeathMatch0":			return "[TDM]";
                case "TeamDeathMatchC0":		return "[TDM]";
                case "TankSuperiority0":		return "[TS]";
                case "Scavenger0":				return "[SC]";
                case "CaptureTheFlag0":			return "[CTF]";
                case "AirSuperiority0":			return "[AS]";
                case "GunMaster0":				return "[GM]";
			}

			return "";
		}

		private string getMapInfo( int index, bool onlyMapName )
		{
			if( currentMaps.Count > 0 && ( index >= 0 && index < currentMaps.Count ) )
			{
				string mapName = GetMapByFilename( currentMaps[index].MapFileName ).PublicLevelName;
         		if( onlyMapName ) return mapName;

         		string modeName = gameModeToShort( currentMaps[index].Gamemode );
         		return mapName + " " + modeName;
			}

			return "";
		}

        private void getChatMessage( string playerName, string chatMessage )
        {
        	Match regex;
        	if( requestTime.CompareTo( DateTime.Now ) < 0 )
        		requestTime = DateTime.Now;

        	regex = Regex.Match( chatMessage, @"^[!|/]maps" );
            if( regex.Success )
            {
            	pluginLogConsole( "Show maplist...", "Notice" );
            	if( currentMaps != null && currentMaps.Count > 0 )
            	{
            		notifyPlayer( "*** Available maps to request ***", playerName );
            		for( int i = 0; i < currentMaps.Count; i++ )
            		{
            			notifyPlayer( "* !request " + i + ": " + getMapInfo( i, false ), playerName );
            		}

            		return;
            	}

            	notifyPlayer( "There is currently no maps in playlist!", playerName );
            }

        	regex = Regex.Match( chatMessage, @"^[!|/]request (\d|[a-z A-Z]+)" );
            if( requestDelay.Equals( DateTime.MinValue ) && regex.Success )
            {
            	pluginLogConsole( "New request...", "Notice" );
            	resetVariables();

            	requestedMap = -1;
            	string[] keys = regex.Groups[regex.Groups.Count - 1].Value.Split( ' ' );

            	if( currentMaps.Count > 0 && keys.Length > 0 )
            	{
	            	if( Regex.IsMatch( keys[0], @"^\d$" ) )
	            	{
	            		requestedMap = Convert.ToInt32( keys[0] );
	            	}
	            	else
	            	{
	            		for( int i = 0; i < currentMaps.Count; i++ )
	            		{
	            			string mapName = getMapInfo( i, true ).ToLower();

	            			for( int j = 0; j < keys.Length; j++ )
	            			{
	            				if( mapName.Contains( keys[j].ToLower() ) )
	            				{
	            					requestedMap = i;
	            					break;
	            				}
	            			}

	            			if( requestedMap != -1 )
	            				break;
	            		}
	            	}
            	}

            	if( requestTime.CompareTo( DateTime.Now ) <= 0 )
            	{
            		if( requestedMap >= 0 && requestedMap < currentMaps.Count )
            		{
		            	requestee = playerName;
		            	requestTime = DateTime.Now.AddSeconds( cooldownDelay );
		            	requestDelay = DateTime.Now.AddSeconds( voteDuration );
		            	notifyAllPlayers( playerName + " requested " + getMapInfo( requestedMap, false ) );
		            	durationTimer = setTimer( votingFinishedEvent, voteDuration );

		            	return;
            		}

           			notifyPlayer( "Requested map was not found!", playerName );
        			return;
            	}

        		notifyPlayer( "Please wait a moment before new request!", playerName );
            }

            regex = Regex.Match( chatMessage, @"^[!|/]approve" );
            if( !requestDelay.Equals( DateTime.MinValue ) && regex.Success )
            {
            	pluginLogConsole( "Approve request...", "Notice" );

            	if( requestDelay.CompareTo( DateTime.Now ) >= 0 )
            	{
	            	if( playerName.Equals( requestee ) && playerCount != approveThreshold )
	            	{
	            		notifyPlayer( "You cannot approve your own request!", playerName );
	            		return;
	            	}

	            	notifyAllPlayers(  "Request was approved by " + playerName );
	            	approvalCount++;

	            	return;
            	}

            	notifyPlayer( "There is no ongoing map request!", playerName );
            }

            regex = Regex.Match( chatMessage, @"^[!|/]abort" );
            if( !requestDelay.Equals( DateTime.MinValue ) && regex.Success )
            {
            	pluginLogConsole( "Abort request...", "Notice" );
            	if( playerName.Equals( requestee ) )
            	{
            		resetVariables();
            		notifyAllPlayers( "Request was aborted by " + playerName );

            		durationTimer.Stop();
            		durationTimer.Dispose();

            		delayTimer.Stop();
            		delayTimer.Dispose();

            		return;
            	}

            	notifyPlayer( "You didn't create current request!", playerName );
            }
        }

        public void OnGlobalChat( string playerName, string chatMessage )
        {
            getChatMessage( playerName, chatMessage );
        }

        public void OnTeamChat( string playerName, string chatMessage, int teamId )
        { 
            getChatMessage( playerName, chatMessage );
        }

        public void OnSquadChat( string playerName, string chatMessage, int teamId, int squadId )
        {
            getChatMessage( playerName, chatMessage );
        }

        #endregion
    }
}