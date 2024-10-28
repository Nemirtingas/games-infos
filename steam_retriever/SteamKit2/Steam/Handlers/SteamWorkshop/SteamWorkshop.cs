﻿/*
 * This file is subject to the terms and conditions defined in
 * file 'license.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using SteamKit2.Internal;

namespace SteamKit2
{
    /// <summary>
    /// This handler is used for requesting files published on the Steam Workshop.
    /// </summary>
    public sealed partial class SteamWorkshop : ClientMsgHandler
    {
        Dictionary<EMsg, Action<IPacketMsg>> dispatchMap;

        internal SteamWorkshop()
        {
            dispatchMap = new Dictionary<EMsg, Action<IPacketMsg>>
            {
                { EMsg.ClientUCMEnumeratePublishedFilesByUserActionResponse, HandleEnumPublishedFilesByAction },
            };
        }


        /// <summary>
        /// Represents the details of an enumeration request used for the local user's files.
        /// </summary>
        public sealed class EnumerationUserDetails
        {
            /// <summary>
            /// Gets or sets the AppID of the workshop to enumerate.
            /// </summary>
            /// <value>
            /// The AppID.
            /// </value>
            public uint AppID { get; set; }

            /// <summary>
            /// Gets or sets the start index.
            /// </summary>
            /// <value>
            /// The start index.
            /// </value>
            public uint StartIndex { get; set; }

            /// <summary>
            /// Gets or sets the user action to filter by.
            /// This value is only used by <see cref="SteamWorkshop.EnumeratePublishedFilesByUserAction"/>.
            /// </summary>
            /// <value>
            /// The user action.
            /// </value>
            public EWorkshopFileAction UserAction { get; set; }
        }
        
        /// <summary>
        /// Enumerates the list of published files for the current logged in user based on user action.
        /// Results are returned in a <see cref="UserActionPublishedFilesCallback"/>.
        /// The returned <see cref="AsyncJob{T}"/> can also be awaited to retrieve the callback result.
        /// </summary>
        /// <param name="details">The specific details of the request.</param>
        /// <returns>The Job ID of the request. This can be used to find the appropriate <see cref="UserActionPublishedFilesCallback"/>.</returns>
        public AsyncJob<UserActionPublishedFilesCallback> EnumeratePublishedFilesByUserAction( EnumerationUserDetails details )
        {
            if ( details == null )
            {
                throw new ArgumentNullException( nameof(details) );
            }

            var enumRequest = new ClientMsgProtobuf<CMsgClientUCMEnumeratePublishedFilesByUserAction>( EMsg.ClientUCMEnumeratePublishedFilesByUserAction );
            enumRequest.SourceJobID = Client.GetNextJobID();

            enumRequest.Body.action = ( int )details.UserAction;
            enumRequest.Body.app_id = details.AppID;
            enumRequest.Body.start_index = details.StartIndex;

            Client.Send( enumRequest );

            return new AsyncJob<UserActionPublishedFilesCallback>( this.Client, enumRequest.SourceJobID );
        }

        /// <summary>
        /// Handles a client message. This should not be called directly.
        /// </summary>
        /// <param name="packetMsg">The packet message that contains the data.</param>
        public override void HandleMsg( IPacketMsg packetMsg )
        {
            if ( packetMsg == null )
            {
                throw new ArgumentNullException( nameof(packetMsg) );
            }

            if (!dispatchMap.TryGetValue(packetMsg.MsgType, out var handlerFunc))
            {
                // ignore messages that we don't have a handler function for
                return;
            }

            handlerFunc( packetMsg );
        }


        #region ClientMsg Handlers
        void HandleEnumPublishedFilesByAction( IPacketMsg packetMsg )
        {
            var response = new ClientMsgProtobuf<CMsgClientUCMEnumeratePublishedFilesByUserActionResponse>( packetMsg );

            var callback = new UserActionPublishedFilesCallback(response.TargetJobID, response.Body);
            Client.PostCallback( callback );
        }
        #endregion

    }
}
