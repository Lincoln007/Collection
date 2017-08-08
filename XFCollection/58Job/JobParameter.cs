using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X.GlodEyes.Collectors.Specialized.JingDong;

namespace XFCollection._58Job
{

    /// <summary>
    /// JobParameter
    /// </summary>
    public class JobParameter:NormalParameter
    {
        /// <summary>
        ///  城市
        /// </summary>
        public string City
        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "City");
            }
            set
            {
                this.SetValueCore((object)value, "City");
            }
        }


        /// <summary>
        /// 职位名称
        /// </summary>
        public string JobName
        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "JobName");
            }
            set
            {
                this.SetValueCore((object)value, "JobName");
            }
        }

        /// <summary>
        /// 发布时间
        /// </summary>
        public string PublishTime
        {
            get
            {
                return this.GetValueCore<string>(string.Empty, "PublishTime");
            }
            set
            {
                this.SetValueCore((object)value, "PublishTime");
            }
        }

    }
}
