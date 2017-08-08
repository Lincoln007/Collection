using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X.GlodEyes.Collectors;

namespace XFCollection.Gov
{
    /// <summary>
    /// ShandongParameter
    /// </summary>
    public class ShandongParameter:Parameter
    {
        /// <summary>
        /// GatherDays
        /// </summary>
        public string GatherDays

        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "GatherDays");
            }

            set
            {
                this.SetValueCore((object)value, "GatherDays");
            }

        }
    }
}
