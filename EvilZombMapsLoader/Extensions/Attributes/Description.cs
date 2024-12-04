using System;
using System.Reflection;

namespace EvilZombMapsLoader.Extensions.Attributes
{
    public class Description : Attribute
    {
        private readonly string _text;

        public Description(string text)
        {
            _text = text;
        }

        public static string GetDescription(Enum en, bool showHexValue = false)
        {
            var enType = en.GetType();

            //Получаем информацию о заданном элементе
            var memberInfos = enType.GetMember(en.ToString());
            
            //Если экземпляр - обычный элемент перечисления
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(Description), false);

                if (attrs.Length > 0)
                {
                    var str = ((Description)attrs[0])._text;

                    if (showHexValue)
                    {
                        return $"{str} (0x{Convert.ToUInt32(en):X8})";
                    }

                    return str;
                }
            }

            return en.ToString();
        }
    }
}
