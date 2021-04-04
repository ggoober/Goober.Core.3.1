using System;

namespace Goober.CommonModels
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SwaggerHideInDocsAttribute : Attribute
    {
        public SwaggerHideInDocsAttribute()
        {

        }

        public SwaggerHideInDocsAttribute(string password)
        {
            Password = password;
        }

        public string Password { get; set; } = "password";
    }
}
