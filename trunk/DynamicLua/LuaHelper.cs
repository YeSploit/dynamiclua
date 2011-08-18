﻿/* Copyright 2011 Niklas Rother
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;

namespace DynamicLua
{
    static class LuaHelper
    {
        public static Random Random { get; private set; }

        static LuaHelper() //static ctor
        {
            Random = new Random();
        }

        //see http://dotnet-snippets.de/dns/passwort-generieren-SID147.aspx
        public static string GetRandomString(int lenght)
        {
            string ret = string.Empty;
            StringBuilder SB = new System.Text.StringBuilder();
            string Content = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < lenght; i++)
                SB.Append(Content[Random.Next(Content.Length)]);
            return SB.ToString();
        }

        public static object UnWrapObject(object wrapped, Lua state, string name = null)
        {
            if (wrapped is LuaTable)
                return new DynamicLuaTable(wrapped as LuaTable, state, name);
            else if (wrapped is LuaFunction)
                return new DynamicLuaFunction(wrapped as LuaFunction);
            else if (wrapped is MulticastDelegate)
                return new DynamicLuaFunction(state.GetFunction(name));
            else
                return wrapped;
        }

        public static object WrapObject(object toWrap, string name, Lua state)
        {
            if (toWrap is MulticastDelegate)
            {
                //We have to deal with a problem here: RegisterFunction does not really create
                //a new function, but a userdata with a __call metamethod. This works fine in all
                //except two cases: When Lua looks for an __index or __newindex metafunction and finds
                //a table or userdata, Lua tries to redirect the read/write operation to that table/userdata.
                //In case of our function that is in reality a userdata this fails. So we have to check
                //for these function and create a very thin wrapper arround this to make Lua see a function instead
                //the real userdata. This is no problem for the other metamethods, these are called independent
                //from their type. (If they are not nil ;))
                MulticastDelegate function = (toWrap as MulticastDelegate);

                if (name.EndsWith("__index") || name.EndsWith("_newindex"))
                {
                    string tmpName = LuaHelper.GetRandomString(8);
                    state.RegisterFunction(tmpName, function.Target, function.Method);
                    state.DoString(String.Format("function {0}(...) return {1}(unpack(arg)) end", name, tmpName), "DynamicLua internal operation");
                }
                else
                    state.RegisterFunction(name, function.Target, function.Method);
                return null;
            }
            else
                return toWrap;
        }
    }
}