﻿using System;
using System.Collections.Generic;
using Knight.Framework.TypeResolve;
using Knight.Core;
using System.Linq;
using System.Reflection;

namespace UnityEngine.UI
{
    public class DataBindingTypeResolve
    {
        public static List<Type> ViewComponentBlackList = new List<Type>()
        {
            typeof(UnityEngine.CanvasRenderer),
            typeof(UnityEngine.UI.MemberBindingAbstract),
            typeof(UnityEngine.UI.MemberBindingOneWay)
        };
            
        public static string PathDot2Oblique(string rSrcPath)
        {
            return rSrcPath.Replace('.', '/');
        }

        public static string PathOblique2Dot(string rSrcPath)
        {
            return rSrcPath.Replace('/', '.');
        }

        public static bool CheckViewComponentBlackList(Type rType)
        {
            bool bIsInBlackList = false;
            for (int i = 0; i < ViewComponentBlackList.Count; i++)
            {
                if (rType.Equals(ViewComponentBlackList[i]))
                {
                    bIsInBlackList = true;
                    break;
                }
            }
            return bIsInBlackList;
        }

        public static List<string> GetAllModelPaths(GameObject rGo, Type rViewPropType)
        {
            return new List<string>(GetViewModelProperties(rGo, rViewPropType).Select(prop =>
            {
                return string.Format("{0}/{1} : {2}", prop.ViewModelType.FullName, prop.MemberName, prop.Member.PropertyType.Name);
            }));
        }

        public static List<string> GetAllViewPaths(GameObject rGo)
        {
            return new List<string>(GetViewProperties(rGo).Select(prop => 
            {
                return string.Format("{0}/{1} : {2}", prop.ViewModelType.FullName, prop.MemberName, prop.Member.PropertyType.Name);
            }));
        }

        public static DataBindingProperty MakeViewDataBindingProperty(GameObject rGo, string rViewPath)
        {
            if (string.IsNullOrEmpty(rViewPath)) return null;

            var rViewPathStrs = rViewPath.Split('/');
            if (rViewPathStrs.Length < 2) return null;

            var rViewClassName = rViewPathStrs[0].Trim();
            var rViewProp = rViewPathStrs[1].Trim();

            var rViewPropStrs = rViewProp.Split(':');
            if (rViewPropStrs.Length < 1) return null;

            var rViewPropName = rViewPropStrs[0].Trim();

            DataBindingProperty rViewDatabindingProp = rGo.GetComponents<Component>()
                            .Where(comp => comp != null &&
                                   comp.GetType().FullName.Equals(rViewClassName) &&
                                   comp.GetType().GetProperty(rViewPropName) != null)
                            .Select(comp =>
                            {
                                return new DataBindingProperty(comp, rViewPropName);
                            })
                            .First();
            return rViewDatabindingProp;
        }

        private static IEnumerable<BindableMember<PropertyInfo>> GetViewProperties(GameObject rGo)
        {
            var rBindableMembers = rGo.GetComponents<Component>()
                .Where(comp => comp != null)
                .SelectMany(comp =>
                {
                    var rType = comp.GetType();
                    return rType
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(prop => new BindableMember<PropertyInfo>(prop, rType));
                })
                .Where(prop => prop.Member.GetSetMethod(false) != null &&
                               prop.Member.GetGetMethod(false) != null &&
                               !ViewComponentBlackList.Contains(prop.ViewModelType) && 
                               !prop.Member.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any()
                      );

            return rBindableMembers;
        }
        
        private static IEnumerable<BindableMember<PropertyInfo>> GetViewModelProperties(GameObject rGo, Type rViewPropType)
        {
            var rBindableMembers = rGo.GetComponentsInParent<ViewModelDataSource>(true)
                .Where(ds => ds != null &&
                       !string.IsNullOrEmpty(ds.ViewModelPath))
                .SelectMany(ds =>
                {
                    var rType = TypeResolveManager.Instance.GetType(ds.ViewModelPath);
                    return rType
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Select(prop => new BindableMember<PropertyInfo>(prop, rType));
                })
                .Where(prop => 
                               prop.Member.PropertyType.Equals(rViewPropType) &&
                               prop.Member.GetSetMethod(false) != null &&
                               prop.Member.GetGetMethod(false) != null &&
                               !ViewComponentBlackList.Contains(prop.ViewModelType) &&
                               !prop.Member.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any()
                      );

            var t = new List<BindableMember<PropertyInfo>>(rBindableMembers);

            return rBindableMembers;
        }
    }
}