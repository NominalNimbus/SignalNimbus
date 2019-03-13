using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace DebugService.Classes
{
        public abstract class CodeParameterBase : ICloneable
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int ID { get; set; }

            protected CodeParameterBase()
            {
                Name = string.Empty;
                Category = string.Empty;
            }

            protected CodeParameterBase(string name, string category, int id)
            {
                Name = name;
                Category = category;
                ID = id;
            }

            public virtual object Clone()
            {
                return MemberwiseClone();
            }

            public override bool Equals(object obj)
            {
                var parameter = obj as CodeParameterBase;
                if (parameter == null)
                    return false;

                return parameter.ID.Equals(ID) && parameter.Category.Equals(Category) && parameter.ID.Equals(ID);
            }
        }

        public class IntParam : CodeParameterBase
        {
            public int Value { get; set; }
            public int MinValue { get; set; }
            public int MaxValue { get; set; }

            public IntParam()
            {
                Value = 0;
                MinValue = int.MinValue;
                MaxValue = int.MaxValue;
            }

            public IntParam(string name, string category, int ID)
                : base(name, category, ID)
            {
                MinValue = int.MinValue;
                MaxValue = int.MaxValue;
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj))
                    return false;

                var parameter = obj as IntParam;
                if (parameter == null)
                    return false;

                return parameter.Value.Equals(Value);
            }
        }

        public class DoubleParam : CodeParameterBase
        {
            public Double Value { get; set; }
            public Double MinValue { get; set; }
            public Double MaxValue { get; set; }

            public DoubleParam()
            {
                Value = 0;
                MinValue = Double.MinValue;
                MaxValue = Double.MaxValue;
            }

            public DoubleParam(string name, string category, int ID)
                : base(name, category, ID)
            {
                MinValue = Double.MinValue;
                MaxValue = Double.MaxValue;
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj))
                    return false;

                var parameter = obj as DoubleParam;
                if (parameter == null)
                    return false;

                return parameter.Value.Equals(Value);
            }
        }

        public class ColorParam : CodeParameterBase
        {
            public Color Value { get; set; }

            public string ColorString
            {
                get { return Value.ToString(); }
                set
                {
                    if (string.IsNullOrEmpty(value))
                        return;

                    try
                    {
                        var color = ColorConverter.ConvertFromString(value);
                        Value = (Color)color;
                    }
                    catch (FormatException)
                    {
                        Value = Colors.Red;
                    }
                }
            }

            public ColorParam()
            {
                Value = Colors.Red;
            }

            public ColorParam(string name, string category, int ID)
                : base(name, category, ID)
            {
                Value = Colors.Red;
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj))
                    return false;

                var parameter = obj as ColorParam;
                if (parameter == null)
                    return false;

                return parameter.Value.Equals(Value);
            }
        }

        public class StringParam : CodeParameterBase
        {
            public string Value { get; set; }
            public List<string> AllowedValues { get; set; }

            public StringParam()
            {
                Value = string.Empty;
                AllowedValues = new List<string>();
            }

            public StringParam(string name, string category, int ID)
                : base(name, category, ID)
            {
                Value = string.Empty;
                AllowedValues = new List<string>();
            }

            public override object Clone()
            {
                var obj = MemberwiseClone() as StringParam;
                obj.AllowedValues = AllowedValues.ToList();
                return obj;
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj))
                    return false;

                var parameter = obj as StringParam;
                if (parameter == null)
                    return false;

                return parameter.Value.Equals(Value);
            }
        }

        public class SeriesParam : CodeParameterBase
        {
            public Color Color { get; set; }

            public string ColorString
            {
                get { return Color.ToString(); }
                set
                {
                    if (string.IsNullOrEmpty(value))
                        return;

                    try
                    {
                        var color = ColorConverter.ConvertFromString(value);
                        Color = (Color)color;
                    }
                    catch (FormatException)
                    {
                        Color = Colors.Red;
                    }
                }
            }

            public int Thickness { get; set; }

            public SeriesParam()
            {
                Color = Colors.Red;
                Thickness = 1;
            }

            public SeriesParam(string name, string category, int ID)
                : base(name, category, ID)
            {
                Color = Colors.Red;
                Thickness = 1;
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj))
                    return false;

                var parameter = obj as SeriesParam;
                if (parameter == null)
                    return false;

                return parameter.Color.Equals(Color) && parameter.Thickness.Equals(Thickness);
            }
        }
}
