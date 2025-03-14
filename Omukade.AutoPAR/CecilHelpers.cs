﻿/*************************************************************************
* Omukade Auto Procedual Assembly Rewriter ("AutoPAR")
* (c) 2023 Hastwell/Electrosheep Networks 
* 
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published
* by the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU Affero General Public License for more details.
* 
* You should have received a copy of the GNU Affero General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************/

using Mono.Cecil;
using Newtonsoft.Json;
using System.Reflection;

namespace Omukade.AutoPAR
{
    internal static class CecilHelpers
    {
        public static void PublicifyType(TypeDefinition type)
        {
            if (type.IsCompilerGenerated()) return;

            if(type.IsNested && !type.IsNestedPublic) type.IsNestedPublic = true;
            if (!type.IsNested && !type.IsPublic) type.IsPublic = true;

            ConstructorInfo JsonIgnoreAttributeConstructor = typeof(JsonIgnoreAttribute).GetConstructor(Type.EmptyTypes);
            MethodReference JsonIgnoreAttributeReference = type.Module.ImportReference(JsonIgnoreAttributeConstructor);
            ConstructorInfo NonSerializedAttributeConstructor = typeof(NonSerializedAttribute).GetConstructor(Type.EmptyTypes);
            MethodReference NonSerializedAttributeReference = type.Module.ImportReference(NonSerializedAttributeConstructor);

            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsCompilerGenerated()) continue;

                bool haveJsonPropertyAttribute = false;

                foreach (CustomAttribute item in field.CustomAttributes)
                {
                    if (item.AttributeType.FullName.Contains("JsonPropertyAttribute"))
                    {
                        haveJsonPropertyAttribute = true;
                        break;
                    }
                }

                if (!field.IsPublic)
                {
                    field.IsPublic = true;
                    if (!haveJsonPropertyAttribute)
                    {
                        field.CustomAttributes.Add(new CustomAttribute(JsonIgnoreAttributeReference));
                        field.CustomAttributes.Add(new CustomAttribute(NonSerializedAttributeReference));
                    }
                }
            }

            foreach (PropertyDefinition prop in type.Properties)
            {
                // Don't use IsCompilerGenerated for properties, as this also includes "{get; set;}" properties.

                bool haveJsonPropertyAttribute = false;

                foreach (CustomAttribute item in prop.CustomAttributes)
                {
                    if (item.AttributeType.FullName.Contains("JsonPropertyAttribute"))
                    {
                        haveJsonPropertyAttribute = true;
                        break;
                    }
                }

                MethodDefinition? getter = prop.GetMethod;
                if (getter != null && !getter.IsPublic)
                {
                    getter.IsPublic = true;
                    if (!haveJsonPropertyAttribute)
                    {
                        prop.CustomAttributes.Add(new CustomAttribute(NonSerializedAttributeReference));
                    }
                }

                MethodDefinition? setter = prop.SetMethod;
                if (setter != null && !setter.IsPublic)
                {
                    setter.IsPublic = true;
                    if (!haveJsonPropertyAttribute)
                    {
                        prop.CustomAttributes.Add(new CustomAttribute(JsonIgnoreAttributeReference));
                    }
                }
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsCompilerGenerated()) continue;

                // Don't touch the .cctor
                if (method.IsConstructor && method.IsStatic) continue;
                if (method.Name == ".cctor") continue;

                if (!method.IsPublic) method.IsPublic = true;
            }

            foreach(TypeDefinition typeDef in type.NestedTypes)
            {
                PublicifyType(typeDef);
            }

            // TODO: Events
            /*
            foreach (EventDefinition eventDef in type.Events)
            {
                if (eventDef.IsCompilerGenerated()) continue;

                if (!eventDef.) eventDef.IsPublic = true;
            }
            */
        }

        public static BaseAssemblyResolver GetResolverSeachingInDirectories(params string[] additionalSearchDirectories)
        {
            DefaultAssemblyResolver dar = new DefaultAssemblyResolver();
            foreach (string searchDirectory in additionalSearchDirectories)
            {
                dar.AddSearchDirectory(searchDirectory);
            }

            return dar;
        }
    }
}
