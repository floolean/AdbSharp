using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public class PropTree : Prop
    {

        internal PropTree()
        {
            Key = "props";
            Name = "props";
            m_Props = new List<Prop>();
        }

        internal void AddProp(string key, string value)
        {

            var prop = FindProp(key);

            if (prop != null)
            {
                prop.Value = value;
            }
            else
            {

                var tokenizedKey = TokenizeKey(key);

                prop = m_Props.Find(p => p.Name == tokenizedKey[0]);

                if (prop != null)
                    prop.AddProp(tokenizedKey, 0, value);
                else
                    m_Props.Add(new Prop(tokenizedKey, 0, value));

            }

        }

        public Prop FindProp(string key)
        {
            return FindProp(TokenizeKey(key), 0);
        }

        public Prop FindProp(string[] keyTokens)
        {

            var prop = m_Props.Find(p => p.Name == keyTokens[0]);

            return prop != null ? prop.FindProp(keyTokens, 0) : null;

        }

    }

    public class Prop
    {

        protected List<Prop> m_Props;

        public string Key
        {
            get;
            internal set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public string Value
        {
            get;
            internal set;
        }

        internal Prop()
        {

        }

        internal Prop(string[] keys, int index, string value)
        {

            Name = keys[index];

            Key = string.Join(".", keys.Take(index + 1));

            if (index == keys.Length - 1)
                Value = value;

            if (index < keys.Length - 1)
            {

                if (m_Props == null)
                {
                    m_Props = new List<Prop>();
                }

                m_Props.Add(new Prop(keys, index + 1, value));

            }

        }

        internal void AddProp(string[] keyTokens, int index = 0, string value = null)
        {

            if (m_Props == null)
                m_Props = new List<Prop>();

            var name = keyTokens[index];

            if (Name == name)
            {

                if (index == keyTokens.Length - 1)
                {
                    Value = value;
                    m_Props = null;
                }
                else
                {
                    AddProp(keyTokens, index + 1, value);
                }

            }
            else
            {

                var prop = m_Props.Find(p => p.Name == name);

                if (prop == null)
                {

                    prop = new Prop(keyTokens, index, value);

                    m_Props.Add(prop);

                }
                else
                {
                    prop.AddProp(keyTokens, index, value);
                }

            }


        }

        internal Prop FindProp(string[] keyTokens, int index = 0)
        {

            var name = keyTokens[index]; ;

            if (Name == name && index == keyTokens.Length - 1)
                return this;

            if (m_Props == null)
                return null;

            foreach (var prop in m_Props)
            {
                if (prop.Name == name && index < keyTokens.Length - 1)
                {
                    return prop.FindProp(keyTokens, index + 1);
                }
            }

            return null;

        }

        public IEnumerable<Prop> Props
        {
            get
            {
                return m_Props;
            }
        }

        public bool HasChildren
        {
            get
            {
                return m_Props != null && m_Props.Count > 0;
            }
        }

        protected string[] TokenizeKey(string key)
        {
            return key.Split('.');
        }

        public override string ToString()
        {
            var value = Value ?? string.Empty;

            return HasChildren ? string.Format("{0} children: {1}", Name, m_Props.Count) : string.Format("{0} = {1}", Name, value);
        }

    }

}
