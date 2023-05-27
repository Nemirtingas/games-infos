using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace epic_retriever
{
    public enum CatalogDataCatalogOfferElementOfferType
    {
        EDITION,
        OTHERS,
        ADD_ON,
        BASE_GAME,
        DLC,
        IN_GAME_PURCHASE,
        VIRTUAL_CURRENCY,
        UNLOCKABLE,
        BUNDLE,
        CONSUMABLE,
    }

    public class CatalogDataCatalogOffersElementItemModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
    }

    public class CatalogDataCatalogOffersElementModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "offerType"), JsonConverter(typeof(StringEnumConverter))]
        public CatalogDataCatalogOfferElementOfferType? OfferType { get; set; }
        [JsonProperty(PropertyName = "items")]
        public List<CatalogDataCatalogOffersElementItemModel> Items { get; set; }
    }

    public class CatalogDataCatalogOffersModel
    {
        [JsonProperty(PropertyName = "elements")]
        public List<CatalogDataCatalogOffersElementModel> Elements { get; set; }
    }

    public class CatalogDataCatalogModel
    {
        [JsonProperty(PropertyName = "catalogOffers")]
        public CatalogDataCatalogOffersModel CatalogOffers { get; set; }
    }

    public class CatalogDataModel
    {
        [JsonProperty(PropertyName = "Catalog")]
        public CatalogDataCatalogModel Catalog { get; set; }
    }

    public class CatalogModel
    {
        [JsonProperty(PropertyName = "data")]
        public CatalogDataModel Data { get; set; }
        //public CatalogExtensionModel Extensions { get; set; }
    }
}
