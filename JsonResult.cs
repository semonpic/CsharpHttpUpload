using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpHttpUpload
{
   public class JsonResult<T>
    {
        public int errcode;	//返回的错误码，为0则没有错误
        public T data;		//返回的内容，泛型
        public String errmsg;
        public JsonResult()
        { 
        
        }
    }
}
