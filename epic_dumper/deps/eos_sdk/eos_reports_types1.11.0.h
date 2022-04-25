#pragma pack(push, 8)

/** The most recent version of the EOS_Reports_SendPlayerBehaviorReport API. */
#define EOS_REPORTS_SENDPLAYERBEHAVIORREPORT_API_001 1

/**
 * Input parameters for the EOS_Reports_SendPlayerBehaviorReport function.
 */
EOS_STRUCT(EOS_Reports_SendPlayerBehaviorReportOptions001, (
	/** API Version: Set this to EOS_REPORTS_SENDPLAYERBEHAVIORREPORT_API_LATEST. */
	int32_t ApiVersion;
	/** Product User ID of the reporting player */
	EOS_ProductUserId ReporterUserId;
	/** Product User ID of the reported player. */
	EOS_ProductUserId ReportedUserId;
	/** Category for the player report. */
	EOS_EPlayerReportsCategory ReportCategory;
	/**
	 * Arbitrary text string associated with the report as UTF-8 encoded null-terminated string.
	 *
	 * The length of the description can be at maximum up to EOS_REPORTS_REPORTDESCRIPTION_MAX_LENGTH bytes
	 * and any excess characters will be truncated upon sending the report.
	 */
	const char* ReportDescription;
));

#pragma pack(pop)
