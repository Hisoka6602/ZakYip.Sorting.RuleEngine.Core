namespace ZakYip.Sorting.RuleEngine.Domain.Constants;

/// <summary>
/// API相关常量定义
/// API-related constants
/// </summary>
public static class ApiConstants {

    /// <summary>
    /// HTTP状态码常量
    /// HTTP status code constants
    /// </summary>
    public static class HttpStatusCodes {
        public const string Success = "200";
        public const string NotFound = "404";
        public const string InternalServerError = "500";
        public const string Error = "ERROR";
    }

    /// <summary>
    /// 内容类型常量
    /// Content type constants
    /// </summary>
    public static class ContentTypes {
        public const string ApplicationJson = "application/json";
        public const string ImageJpeg = "image/jpeg";
        public const string ImagePng = "image/png";
        public const string ImageGif = "image/gif";
        public const string ImageBmp = "image/bmp";
        public const string ImageWebp = "image/webp";
    }

    /// <summary>
    /// WCS API端点常量
    /// WCS API endpoint constants
    /// </summary>
    public static class WcsEndpoints {
        public const string SortingUpload = "/api/sorting/upload";
        public const string ParcelScan = "/api/parcel/scan";
        public const string ChuteRequest = "/api/chute/request";
        public const string ImageUpload = "/api/image/upload";
    }

    /// <summary>
    /// 旺店通WMS API相关常量
    /// WDT WMS API constants
    /// </summary>
    public static class WdtWmsApi {
        public const string RouterEndpoint = "/openapi/router";

        /// <summary>
        /// API方法名称
        /// API method names
        /// </summary>
        public static class Methods {
            public const string WeighUpload = "wms.weigh.upload";
            public const string ParcelScan = "wms.parcel.scan";
            public const string ParcelQuery = "wms.parcel.query";
            public const string ImageUpload = "wms.parcel.image.upload";
        }

        /// <summary>
        /// API通用参数值
        /// Common API parameter values
        /// </summary>
        public static class CommonParams {
            public const string FormatJson = "json";
            public const string Version = "1.0";
        }
    }

    /// <summary>
    /// 聚水潭ERP API相关常量
    /// Jushuituan ERP API constants
    /// </summary>
    public static class JushuitanErpApi {
        public const string RouterEndpoint = "/open/api/open/router";

        /// <summary>
        /// API方法名称
        /// API method names
        /// </summary>
        public static class Methods {
            public const string WeighingUpload = "weighing.upload";
            public const string OrdersSingleQuery = "orders.single.query";
            public const string LogisticUpload = "logistic.upload";
        }

        /// <summary>
        /// API通用参数值
        /// Common API parameter values
        /// </summary>
        public static class CommonParams {

            /// <summary>
            /// 字符集UTF-8
            /// Character set UTF-8
            /// </summary>
            public const string CharsetUtf8 = "utf-8";

            /// <summary>
            /// 自动分配格口
            /// Auto-assign chute
            /// </summary>
            public const string AutoAssign = "AUTO";
        }
    }

    /// <summary>
    /// 邮政分揽投机构API相关常量
    /// Postal Collection/Delivery Institution API constants
    /// </summary>
    public static class PostCollectionApi {
        public const string RouterEndpoint = "/api/post/collection";

        /// <summary>
        /// API端点
        /// API endpoints
        /// </summary>
        public static class Endpoints {
            public const string WeighingUpload = "/weighing/upload";
            public const string ParcelQuery = "/parcel/query";
            public const string ScanUpload = "/scan/upload";
        }

        /// <summary>
        /// API通用参数值
        /// Common API parameter values
        /// </summary>
        public static class CommonParams {
            public const string Version = "1.0";
            public const string FormatJson = "json";
        }
    }

    /// <summary>
    /// 邮政处理中心API相关常量
    /// Postal Processing Center API constants
    /// </summary>
    public static class PostProcessingCenterApi {
        public const string RouterEndpoint = "/api/post/processing";

        /// <summary>
        /// API端点
        /// API endpoints
        /// </summary>
        public static class Endpoints {
            public const string WeighingUpload = "/weighing/upload";
            public const string RoutingQuery = "/routing/query";
            public const string SortingResultUpload = "/sorting/result";
            public const string ScanUpload = "/scan/upload";
        }

        /// <summary>
        /// API通用参数值
        /// Common API parameter values
        /// </summary>
        public static class CommonParams {
            public const string Version = "1.0";
            public const string FormatJson = "json";
        }
    }
}
