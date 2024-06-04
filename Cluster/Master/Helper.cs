using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Master
{
    public class Helper
    {
        public static int BinarySearch(List<NodeResponse> list, int hashVal){
            int left = 0;
            int right = list.Count - 1;
            int ret = 0;
            while(left <= right){
                int mid = left + (right - left) / 2;
                if(list[mid].hash == hashVal){
                    return mid;
                }
                if(mid != list.Count -1){
                    if(list[mid].hash < hashVal && list[mid + 1].hash > hashVal){
                        ret = mid +1 ;
                        break;
                    }
                }
                if(list[mid].hash < hashVal){
                    left = mid + 1;
                }
                else{
                    right = mid - 1;
                }
            }
            if (ret == list.Count){
                return 0;
            }else {
                return ret;
            }
        }
    }
}