using dnlib.DotNet;
using System;
using System.Collections.Generic;
using NetControlFlow.Logging;
using NetControlFlow.Models;

namespace NetControlFlow.Deobfuscators
{
    public abstract class BaseDeobfuscator
    {
        protected ModuleDef Module { get; set; }
        protected string Name { get; set; }

        public BaseDeobfuscator(ModuleDef module)
        {
            Module = module;
        }

        public abstract void Deobfuscate();

        protected virtual void CleanAttributes()
        {
            try
            {
                var toRemove = new List<CustomAttribute>();
                foreach (var attr in Module.Assembly.CustomAttributes)
                {
                    if (attr.TypeFullName.Contains("SuppressIldasm") ||
                        attr.TypeFullName.Contains("ObfuscatedByAttribute") ||
                        attr.TypeFullName.Contains("Confuser"))
                    {
                        toRemove.Add(attr);
                    }
                }

                foreach (var attr in toRemove)
                    Module.Assembly.CustomAttributes.Remove(attr);

                if (toRemove.Count > 0)
                    LogManager.LogOperation($"Removed {toRemove.Count} obfuscator attributes");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error cleaning attributes in {Name}", ex);
            }
        }

        protected virtual void CleanNamespaces()
        {
            try
            {
                var suspiciousNamespaces = new List<TypeDef>();
                foreach (var type in Module.Types)
                {
                    if (string.IsNullOrWhiteSpace(type.Namespace) ||
                        type.Namespace.Contains("\x00") ||
                        type.Namespace.Contains("＜") ||
                        Char.IsControl(type.Namespace[0]))
                    {
                        suspiciousNamespaces.Add(type);
                        type.Namespace = "Deobfuscated";
                    }
                }

                if (suspiciousNamespaces.Count > 0)
                    LogManager.LogOperation($"Cleaned {suspiciousNamespaces.Count} suspicious namespaces");
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Error cleaning namespaces in {Name}", ex);
            }
        }
    }
}