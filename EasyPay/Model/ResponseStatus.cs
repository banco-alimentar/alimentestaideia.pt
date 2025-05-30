/*
 * Easypay Payments API
 *
 * <a href='https://www.easypay.pt/en/legal-terms-and-conditions/' class='item'>Terms conditions and legal terms</a><br><a href='https://www.easypay.pt/en/privacy-and-data-protection-policy/' class='item'>Privacy Policy</a>
 *
 * The version of the OpenAPI document: 2.0
 * Contact: tec@easypay.pt
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = Easypay.Rest.Client.Client.OpenAPIDateConverter;

namespace Easypay.Rest.Client.Model
{
    /// <summary>
    /// Indicates the response status of the API request. Possible values are:      - ok: Returned for all successful responses with a 2xx status code.     - error: Returned for any unsuccessful response with a non-2xx status code.  This field helps to quickly identify the overall outcome of the API request.
    /// </summary>
    /// <value>Indicates the response status of the API request. Possible values are:      - ok: Returned for all successful responses with a 2xx status code.     - error: Returned for any unsuccessful response with a non-2xx status code.  This field helps to quickly identify the overall outcome of the API request.</value>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResponseStatus
    {
        /// <summary>
        /// Enum Ok for value: ok
        /// </summary>
        [EnumMember(Value = "ok")]
        Ok = 1,

        /// <summary>
        /// Enum Error for value: error
        /// </summary>
        [EnumMember(Value = "error")]
        Error = 2
    }

}
