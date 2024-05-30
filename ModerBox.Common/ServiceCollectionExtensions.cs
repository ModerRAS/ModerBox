using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Common {
    public static class ServiceCollectionExtensions {
        public static void AddCommonServices(this IServiceCollection collection) {
            collection.AddTransient<DataWriter>();
        }
    }
}
