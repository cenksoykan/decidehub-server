﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Decidehub.Web.Resources.Controllers.Api {
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ApiSharePollStartPollController {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ApiSharePollStartPollController() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Decidehub.Web.Resources.Controllers.Api.ApiSharePollStartPollController", typeof(ApiSharePollStartPollController).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Aktif yetki meclisi oylaması mevcuttur..
        /// </summary>
        public static string AuthorityPollActivePollError {
            get {
                return ResourceManager.GetString("AuthorityPollActivePollError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oylama bitmiştir.
        /// </summary>
        public static string PollCompleted {
            get {
                return ResourceManager.GetString("PollCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oylama oy kullanmaya hazır değildir.
        /// </summary>
        public static string PollIsNotReady {
            get {
                return ResourceManager.GetString("PollIsNotReady", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PollNotFound.
        /// </summary>
        public static string PollNotFound {
            get {
                return ResourceManager.GetString("PollNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bu oylamaya ait kayıdınız bulunmaktadır!.
        /// </summary>
        public static string PollRecordExist {
            get {
                return ResourceManager.GetString("PollRecordExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oylama tipi Paylaşım Oylaması olmayan oylama..
        /// </summary>
        public static string PollTypeNotSharePollStartPoll {
            get {
                return ResourceManager.GetString("PollTypeNotSharePollStartPoll", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oy kullanmak için yeterli yetkiye sahip değilsiniz.
        /// </summary>
        public static string PollVoterError {
            get {
                return ResourceManager.GetString("PollVoterError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Aktif Paylaşım Oylaması mevcuttur.
        /// </summary>
        public static string SharePollActivePollError {
            get {
                return ResourceManager.GetString("SharePollActivePollError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Paylaşım Oylaması Başlatma Oylaması.
        /// </summary>
        public static string SharePollStart {
            get {
                return ResourceManager.GetString("SharePollStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Aktif Paylaşım Oylaması Başlatma Oylaması mevcuttur.
        /// </summary>
        public static string SharePollStartActivePollError {
            get {
                return ResourceManager.GetString("SharePollStartActivePollError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Oylama başladıktan sonra sisteme eklendiğiniz için bu oylamada oy kullanamazsınız!.
        /// </summary>
        public static string UserCannotVoteAfterAddedPollStart {
            get {
                return ResourceManager.GetString("UserCannotVoteAfterAddedPollStart", resourceCulture);
            }
        }
    }
}
