// Copyright Epic Games, Inc. All Rights Reserved.

#pragma pack(push, 8)

/** The most recent version of the EOS_Ecom_CatalogOffer struct. */
#define EOS_ECOM_CATALOGOFFER_API_003 3

/** Timestamp value representing an undefined ExpirationTimestamp for EOS_Ecom_CatalogOffer */
#define EOS_ECOM_CATALOGOFFER_EXPIRATIONTIMESTAMP_UNDEFINED -1

/**
 * Contains information about a single offer within the catalog. Instances of this structure are
 * created by EOS_Ecom_CopyOfferByIndex. They must be passed to EOS_Ecom_CatalogOffer_Release.
 * Prices are stored in the lowest denomination for the associated currency.  If CurrencyCode is
 * "USD" then a price of 299 represents "$2.99".
 */
EOS_STRUCT(EOS_Ecom_CatalogOffer003, (
	/** API Version: Set this to EOS_ECOM_CATALOGOFFER_API_LATEST. */
	int32_t ApiVersion;
	/**
	 * The index of this offer as it exists on the server.
	 * This is useful for understanding pagination data.
	 */
	int32_t ServerIndex;
	/** Product namespace in which this offer exists */
	const char* CatalogNamespace;
	/** The ID of this offer */
	EOS_Ecom_CatalogOfferId Id;
	/** Localized UTF-8 title of this offer */
	const char* TitleText;
	/** Localized UTF-8 description of this offer */
	const char* DescriptionText;
	/** Localized UTF-8 long description of this offer */
	const char* LongDescriptionText;
	/**
	 * Deprecated.
	 * EOS_Ecom_CatalogOffer::TechnicalDetailsText has been deprecated.
	 * EOS_Ecom_CatalogItem::TechnicalDetailsText is still valid.
	 */
	const char* TechnicalDetailsText_DEPRECATED;
	/** The Currency Code for this offer */
	const char* CurrencyCode;
	/**
	 * If this value is EOS_Success then OriginalPrice, CurrentPrice, and DiscountPercentage contain valid data.
	 * Otherwise this value represents the error that occurred on the price query.
	 */
	EOS_EResult PriceResult;
	/** The original price of this offer as a 32-bit number is deprecated. */
	uint32_t OriginalPrice_DEPRECATED;
	/** The current price including discounts of this offer as a 32-bit number is deprecated.. */
	uint32_t CurrentPrice_DEPRECATED;
	/** A value from 0 to 100 define the percentage of the OrignalPrice that the CurrentPrice represents */
	uint8_t DiscountPercentage;
	/** Contains the POSIX timestamp that the offer expires or -1 if it does not expire */
	int64_t ExpirationTimestamp;
	/** The number of times that the requesting account has purchased this offer. */
	uint32_t PurchasedCount;
	/**
	 * The maximum number of times that the offer can be purchased.
	 * A negative value implies there is no limit.
	 */
	int32_t PurchaseLimit;
	/** True if the user can purchase this offer. */
	EOS_Bool bAvailableForPurchase;
	/** The original price of this offer as a 64-bit number. */
	uint64_t OriginalPrice64;
	/** The current price including discounts of this offer as a 64-bit number. */
	uint64_t CurrentPrice64;
));

#pragma pack(pop)
