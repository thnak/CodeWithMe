﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BusinessModels.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class AppLang {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public AppLang() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BusinessModels.Resources.AppLang", typeof(AppLang).Assembly);
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
        ///   Looks up a localized string similar to Thành công.
        /// </summary>
        public static string Success {
            get {
                return ResourceManager.GetString("Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Người dùng đã tồn tại.
        /// </summary>
        public static string User_is_already_exists {
            get {
                return ResourceManager.GetString("User is already exists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Người dùng không tồn tại.
        /// </summary>
        public static string User_is_not_exists {
            get {
                return ResourceManager.GetString("User is not exists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tên người dùng không hợp lệ.
        /// </summary>
        public static string User_name_is_not_valid {
            get {
                return ResourceManager.GetString("User name is not valid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cập nhật người dùng không thành công.
        /// </summary>
        public static string User_update_failed {
            get {
                return ResourceManager.GetString("User update failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cập nhật người dùng thành công.
        /// </summary>
        public static string User_updated_successfully {
            get {
                return ResourceManager.GetString("User updated successfully", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tài khoản hoặc mật khẩu không đúng.
        /// </summary>
        public static string Username_or_password_incorrect {
            get {
                return ResourceManager.GetString("Username or password incorrect", resourceCulture);
            }
        }
    }
}
