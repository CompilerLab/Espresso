﻿//MIT, 2015-2017, WinterDev, EngineKit, brezza92

using System;
using System.Collections.Generic;
using System.IO;

namespace Espresso
{

    public abstract class JsTypeMemberDefinition
    {
        string _mbname;
        JsMemberKind _memberKind;
        JsTypeDefinition _owner;
        int _memberId;
        internal INativeRef _nativeProxy;
        public JsTypeMemberDefinition(string mbname, JsMemberKind memberKind)
        {
            this._mbname = mbname;
            this._memberKind = memberKind;
        }
        public bool IsRegisterd
        {
            get
            {
                return this._nativeProxy != null;
            }
        }

        public string MemberName
        {
            get
            {
                return this._mbname;
            }
        }
        public JsMemberKind MemberKind
        {
            get
            {
                return this._memberKind;
            }
        }
        public void SetOwner(JsTypeDefinition owner)
        {
            this._owner = owner;
        }
        protected static void WriteUtf16String(string str, BinaryWriter writer)
        {
            char[] charBuff = str.ToCharArray();
            writer.Write((short)charBuff.Length);
            writer.Write(charBuff);
        }

        public int MemberId
        {
            get
            {
                return this._memberId;
            }
        }
        public void SetMemberId(int memberId)
        {
            this._memberId = memberId;
        }

    }
    public class JsTypeDefinition : JsTypeMemberDefinition
    {
        //store definition for js
        List<JsFieldDefinition> fields = new List<JsFieldDefinition>();
        List<JsMethodDefinition> methods = new List<JsMethodDefinition>();
        List<JsPropertyDefinition> props = new List<JsPropertyDefinition>();

        public JsTypeDefinition(string typename)
            : base(typename, JsMemberKind.Type)
        {

        }

        public void AddMember(JsMethodDefinition methodDef)
        {
            methodDef.SetOwner(this);
            methods.Add(methodDef);
        }
        public void AddMember(JsPropertyDefinition propDef)
        {
            propDef.SetOwner(this);
            props.Add(propDef);

        }
        /// <summary>
        /// serialization this typedefinition to binary format and 
        /// send to native side
        /// </summary>
        /// <param name="writer"></param>
        internal void WriteDefinitionToStream(BinaryWriter writer)
        {
            //----------------------
            //this is our custom protocol/convention with the MiniJsBridge            
            //we may change this in the future
            //eg. use json serialization/deserialization 
            //----------------------

            //1. kind/flags
            writer.Write((short)this.MemberId);
            //2. member id
            writer.Write((short)0);
            //3. typename                         
            WriteUtf16String(this.MemberName, writer);

            //4. num of field
            int j = fields.Count;
            writer.Write((short)j);
            for (int i = 0; i < j; ++i)
            {
                JsFieldDefinition fielddef = fields[i];
                //field flags
                writer.Write((short)0);

                //*** field id -- unique field id within one type
                writer.Write((short)fielddef.MemberId);

                //field name
                WriteUtf16String(fielddef.MemberName, writer);
            }
            //------------------
            j = methods.Count;
            writer.Write((short)j);
            for (int i = 0; i < j; ++i)
            {
                JsMethodDefinition methoddef = methods[i];
                //method flags
                writer.Write((short)0);
                //id
                writer.Write((short)methoddef.MemberId);
                //method name
                WriteUtf16String(methoddef.MemberName, writer);
            }

            //property
            j = props.Count;
            writer.Write((short)j);
            for (int i = 0; i < j; ++i)
            {
                JsPropertyDefinition property = this.props[i];
                //flags
                writer.Write((short)0);
                //id
                writer.Write((short)property.MemberId);
                //name
                WriteUtf16String(property.MemberName, writer);
            }

        }

        internal List<JsFieldDefinition> GetFields()
        {
            return this.fields;
        }
        internal List<JsMethodDefinition> GetMethods()
        {
            return this.methods;
        }
        internal List<JsPropertyDefinition> GetProperties()
        {
            return this.props;
        }
    }

    public enum JsMemberKind
    {
        Field,
        Method,
        Event,
        Property,
        Indexer,
        PropertyGet,
        PropertySet,
        IndexerGet,
        IndexerSet,
        Type
    }

    public class JsFieldDefinition : JsTypeMemberDefinition
    {
        public JsFieldDefinition(string fieldname)
            : base(fieldname, JsMemberKind.Field)
        {

        }
    }

    public class JsPropertyDefinition : JsTypeMemberDefinition
    {
        public JsPropertyDefinition(string name)
            : base(name, JsMemberKind.Property)
        {
            //create blank property and we can add getter/setter later

        }
        public JsPropertyDefinition(string name, JsMethodCallDel getter, JsMethodCallDel setter)
            : base(name, JsMemberKind.Property)
        {

            if (getter != null)
            {
                this.GetterMethod = new JsPropertyGetDefinition(name, getter);
            }
            if (setter != null)
            {
                this.SetterMethod = new JsPropertySetDefinition(name, setter);
            }
        }
        public JsPropertyDefinition(string name, System.Reflection.PropertyInfo propInfo)
            : base(name, JsMemberKind.Property)
        {

#if NET20

            var getter = propInfo.GetGetMethod(true);
            if (getter != null)
            {
                this.GetterMethod = new JsPropertyGetDefinition(name, getter);
            }
            var setter = propInfo.GetSetMethod(true);
            if (setter != null)
            {
                this.SetterMethod = new JsPropertySetDefinition(name, setter);
            }
#else
            var getter = propInfo.GetMethod;
            if (getter != null)
            {
                this.GetterMethod = new JsPropertyGetDefinition(name, getter);
            }
            var setter = propInfo.SetMethod;
            if (setter != null)
            {
                this.SetterMethod = new JsPropertySetDefinition(name, setter);
            }
#endif

        }
        public JsPropertyGetDefinition GetterMethod
        {
            get;
            set;
        }
        public JsPropertySetDefinition SetterMethod
        {
            get;
            set;
        }
        public bool IsIndexer { get; set; }
    }

    public class JsPropertyGetDefinition : JsMethodDefinition
    {

        public JsPropertyGetDefinition(string name, JsMethodCallDel getter)
            : base(name, getter)
        {
        }
        public JsPropertyGetDefinition(string name, System.Reflection.MethodInfo getterMethod)
            : base(name, getterMethod)
        {
        }
    }

    public class JsPropertySetDefinition : JsMethodDefinition
    {

        public JsPropertySetDefinition(string name, JsMethodCallDel setter)
            : base(name, setter)
        {
        }
        public JsPropertySetDefinition(string name, System.Reflection.MethodInfo setterMethod)
            : base(name, setterMethod)
        {
        }
    }

    public class JsMethodDefinition : JsTypeMemberDefinition
    {

        JsMethodCallDel methodCallDel;
        System.Reflection.MethodInfo method;
        System.Reflection.ParameterInfo[] parameterInfoList;
        System.Type methodReturnType;
        bool isReturnTypeVoid;

        public JsMethodDefinition(string methodName, JsMethodCallDel methodCallDel)
            : base(methodName, JsMemberKind.Method)
        {
            this.methodCallDel = methodCallDel;
        }

        public JsMethodDefinition(string methodName, System.Reflection.MethodInfo method)
            : base(methodName, JsMemberKind.Method)
        {
            this.method = method;
            //analyze expected arg type
            //and conversion plan
            this.parameterInfoList = method.GetParameters();
            this.methodReturnType = method.ReturnType;
            this.isReturnTypeVoid = this.methodReturnType == typeof(void);
        }

        public void InvokeMethod(ManagedMethodArgs args)
        {
            if (method != null)
            {
                //invoke method

                object thisArg = args.GetThisArg();

                //actual input arg count
                int actualArgCount = args.ArgCount;
                //prepare parameters
                int expectedParameterCount = parameterInfoList.Length;
                object[] parameters = new object[expectedParameterCount];

                //TODO: review here
                //check exact number
                int lim = Math.Min(actualArgCount, expectedParameterCount);
                //fill from the begin 
                for (int i = 0; i < lim; ++i)
                {
                    object arg = args.GetArgAsObject(i);
                    //if type not match then covert it
                    if (arg is JsFunction)
                    {
                        //convert to deledate
                        //check if the target need delegate
                        var func = (JsFunction)arg;
                        //create delegate for a specific target type***
                        parameters[i] = func.MakeDelegate(parameterInfoList[i].ParameterType);
                    }
                    else
                    {
                        parameters[i] = arg;
                    }
                }

                //send to .net 
                object result = this.method.Invoke(thisArg, parameters);

                if (isReturnTypeVoid)
                {
                    //set to undefine because of void
                    args.SetResultUndefined();
                }
                else
                {
                    args.SetResultObj(result);
                }
            }
            else
            {
                methodCallDel(args);
            }
        }


        internal System.Reflection.ParameterInfo[] Parameters { get { return parameterInfoList; } }
        internal System.Reflection.MethodInfo MethodInfo { get { return method; } }
        internal JsMethodCallDel JsMetDelegate { get { return methodCallDel; } }

#if DEBUG
        public override string ToString()
        {
            return this.MemberName;
        }
#endif
    }

    public delegate void JsMethodCallDel(ManagedMethodArgs args);

    public struct JsArgValue
    {
        JsValue jsvalue;
    }


    public struct ManagedMethodArgs
    {
        IntPtr metArgsPtr;
        JsContext context;
        public ManagedMethodArgs(JsContext context, IntPtr metArgsPtr)
        {
            this.context = context;
            this.metArgsPtr = metArgsPtr;
        }
        public int ArgCount
        {
            get
            {
                return NativeV8JsInterOp.ArgCount(this.metArgsPtr);
            }
        }
        public object GetThisArg()
        {
            JsValue output = new JsValue();
            NativeV8JsInterOp.ArgGetThis(this.metArgsPtr, ref output);
            return this.context.Converter.FromJsValue(ref output);
        }
        public object GetArgAsObject(int index)
        {
            JsValue output = new JsValue();
            NativeV8JsInterOp.ArgGetObject(this.metArgsPtr, index, ref output);
            return this.context.Converter.FromJsValue(ref output);
        }
        //--------------------------------------------------------------------
        public void SetResult(bool value)
        {
            NativeV8JsInterOp.ResultSetBool(metArgsPtr, value);
        }
        public void SetResult(int value)
        {
            NativeV8JsInterOp.ResultSetInt32(metArgsPtr, value);
        }
        public void SetResult(string value)
        {
            NativeV8JsInterOp.ResultSetString(metArgsPtr, value);
        }
        public void SetResult(double value)
        {
            NativeV8JsInterOp.ResultSetDouble(metArgsPtr, value);
        }
        public void SetResult(float value)
        {
            NativeV8JsInterOp.ResultSetFloat(metArgsPtr, value);
        }
        public void SetResultNull()
        {
            NativeV8JsInterOp.ResultSetJsNull(metArgsPtr);
        }
        public void SetResultUndefined()
        {
            //TODO: review here again
            NativeV8JsInterOp.ResultSetJsVoid(metArgsPtr);
        }
        public void SetResultObj(object result)
        {
            JsValue output = new JsValue();
            this.context.Converter.AnyToJsValue(result, ref output);
            NativeV8JsInterOp.ResultSetValue(metArgsPtr, ref output);
        }

        public void SetResultObj(object result, JsTypeDefinition jsTypeDef)
        {
            if (!jsTypeDef.IsRegisterd)
            {
                this.context.RegisterTypeDefinition(jsTypeDef);
            }

            INativeScriptable proxy = this.context.CreateWrapper(result, jsTypeDef);
            JsValue output = new JsValue();
            this.context.Converter.ToJsValue(proxy, ref output);
            NativeV8JsInterOp.ResultSetValue(metArgsPtr, ref output);
        }
        public void SetResultAutoWrap<T>(T result)
            where T : class, new()
        {

            Type actualType = result.GetType();
            JsTypeDefinition jsTypeDef = this.context.GetJsTypeDefinition(actualType);
            INativeScriptable proxy = this.context.CreateWrapper(result, jsTypeDef);
            JsValue output = new JsValue();
            this.context.Converter.ToJsValue(proxy, ref output);
            NativeV8JsInterOp.ResultSetValue(metArgsPtr, ref output);

        }

    }
}