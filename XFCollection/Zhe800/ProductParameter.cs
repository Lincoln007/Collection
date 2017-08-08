using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X.GlodEyes.Collectors.Specialized.JingDong;

namespace XFCollection.Zhe800
{
    /// <summary>
    /// ProductParameter
    /// </summary>
    public class ProductParameter:NormalParameter
    {
        /// <summary>
        /// 关键词
        /// </summary>
        public string KeyWord
        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "KeyWord");
            }
            set
            {
                this.SetValueCore((object)value, "KeyWord");
            }
        }


        /// <summary>
        /// SearchPage
        /// </summary>
        public string SearchPage
        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "SearchPage");
            }

            set
            {
                this.SetValueCore((object)value, "SearchPage");
            }

        }

    }
}
