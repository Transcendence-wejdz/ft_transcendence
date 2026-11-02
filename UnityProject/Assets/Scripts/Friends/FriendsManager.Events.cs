using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Multiplayer;

// Friends service events: relationships, presence, messages and our own name, relayed to the lists.
public static partial class FriendsManager
{
    private static void RaiseChanged()
    {
        OnChanged?.Invoke();
    }

    private static void HandlePlayerNameChanged(string name)
    {
        OwnName = name;
        RaiseChanged();
    }

    private static void HandleRelationshipDeleted(IRelationshipDeletedEvent deleted)
    {
        RaiseChanged();
    }

    private static void HandlePresenceUpdated(IPresenceUpdatedEvent presence)
    {
        RaiseChanged();
    }

    private static void SubscribeToService()
    {
        ServicesAuth.OnPlayerNameChanged += HandlePlayerNameChanged;
        FriendsService.Instance.RelationshipAdded += HandleRelationshipAdded;
        FriendsService.Instance.RelationshipDeleted += HandleRelationshipDeleted;
        FriendsService.Instance.PresenceUpdated += HandlePresenceUpdated;
        FriendsService.Instance.MessageReceived += HandleMessage;
    }

    // The other member is the Source of an incoming request; our own sent requests also raise RelationshipAdded.
    private static void HandleRelationshipAdded(IRelationshipAddedEvent added)
    {
        Relationship relationship = added.Relationship;

        RaiseChanged();
        if (relationship == null || relationship.Type != RelationshipType.FriendRequest)
            return;
        if (relationship.Member.Role != MemberRole.Source)
            return;
        OnFriendRequestReceived?.Invoke(relationship.Member.Id, ShortName(relationship.Member.Profile?.Name));
    }

    // Ignores anything that isn't a friend invite; a code-less invite is valid (its code follows in a second message).
    private static void HandleMessage(IMessageReceivedEvent message)
    {
        FriendInvite invite;

        try
        {
            invite = message.GetAs<FriendInvite>();
        }
        catch (Exception)
        {
            return;
        }

        if (invite == null || string.IsNullOrEmpty(invite.SenderName))
            return;
        invite.SenderId = message.UserId;
        OnInviteReceived?.Invoke(invite);
    }
}
