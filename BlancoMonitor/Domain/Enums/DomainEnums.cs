namespace BlancoMonitor.Domain.Enums;

public enum Severity
{
    Info,
    Notice,
    Warning,
    Critical
}

public enum MonitorStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public enum ScenarioActionType
{
    Navigate,
    Search,
    Wait,
    FollowLinks,
    ClickLink,
    ScrollPage
}

public enum TrendDirection
{
    Stable,
    Improving,
    Degrading,
    Insufficient
}

public enum ComparisonOperator
{
    GreaterThan,
    LessThan
}

public enum IssueCategory
{
    Performance,
    Availability,
    StatusCode,
    Timeout,
    Content,
    Security,
    Redirect
}

public enum EvidenceType
{
    Screenshot,
    ResponseBody,
    Headers,
    HarFile
}

public enum RequestCategory
{
    Html,
    Api,
    JavaScript,
    Css,
    Image,
    Font,
    Analytics,
    ThirdParty,
    Other
}

public enum ConfidenceLevel
{
    Suspected,
    Confirmed,
    Persistent
}

public enum PageType
{
    Homepage,
    Product,
    Search,
    Category,
    Checkout,
    Api,
    Static,
    Unknown
}
