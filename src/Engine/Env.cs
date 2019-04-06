using System.Collections.Generic;

using Mal;
using eValue = Mal.Types.eValue;
using eSymbol = Mal.Types.eSymbol;
using eList = Mal.Types.eList;

namespace Mal
{
    public class env
    {
        public class Env
        {
            Env outer = null;
            Dictionary<string, eValue> data = new Dictionary<string, eValue>();

            public Env(Env outer)
            {
                this.outer = outer;
            }

            public Env(Env outer, eList binds, eList exprs)
            {
                this.outer = outer;
                for (int i = 0; i < binds.size(); i++)
                {
                    string sym = ((eSymbol) binds.nth(i)).getName();
                    if (sym == "&")
                    {
                        data[((eSymbol) binds.nth(i + 1)).getName()] = exprs.slice(i);
                        break;
                    }
                    else
                    {
                        data[sym] = exprs.nth(i);
                    }
                }
            }

            public Env find(eSymbol key)
            {
                if (data.ContainsKey(key.getName()))
                {
                    return this;
                }
                else if (outer != null)
                {
                    return outer.find(key);
                }
                else
                {
                    return null;
                }
            }

            public eValue get(eSymbol key)
            {
                Env e = find(key);
                if (e == null)
                {
                    throw new Mal.Types.eException(
                        "'" + key.getName() + "' not found");
                }
                else
                {
                    return e.data[key.getName()];
                }
            }

            public Env set(eSymbol key, eValue value)
            {
                data[key.getName()] = value;
                return this;
            }
        }
    }
}
