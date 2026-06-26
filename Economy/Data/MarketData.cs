using System;

namespace Dystopia.Economy.Data
{
    [Serializable] public class MarketCardData
    {
        public int level;
        public int tier;
        public int resonance;
        public int duplicateCount;
    }

    [Serializable] public class MarketListing
    {
        public string         listingId;
        public string         sellerId;
        public string         sellerName;
        public string         itemInstanceId;
        public string         cardId;
        public string         listingType;
        public int            price;
        public int            listingFee;
        public string         listedAt;
        public string         cooldownEndsAt;
        public bool           buyable;
        public MarketCardData cardData;
    }

    [Serializable] public class ListCardResult
    {
        public string listingId;
        public int    fee;
        public string cooldownEndsAt;
        public string listingType;
    }

    [Serializable] public class BuyCardResult
    {
        public bool           success;
        public string         cardId;
        public string         listingType;
        public int            price;
        public int            newGoldBalance;
        public bool           isNewCard;
        public string         newItemInstanceId;
        public MarketCardData cardData;
    }

    [Serializable] public class DelistCardResult
    {
        public bool           success;
        public string         listingType;
        public string         cardId;
        public string         newItemInstanceId;
        public string         cooldownUntil;
        public MarketCardData savedCardData;
        public string         originalItemInstanceId;
    }

    [Serializable] internal class MarketListingsWrapper
    {
        public MarketListing[] listings;
    }
}
