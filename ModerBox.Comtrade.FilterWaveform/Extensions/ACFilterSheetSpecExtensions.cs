using ModerBox.Comtrade.FilterWaveform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Extensions {
    public static class ACFilterSheetSpecExtensions {
        public static ACFilterSheetSpec MergeFilterSwitchingTimeDTO(this ACFilterSheetSpec aCFilterSheetSpec, FilterSwitchingTimeDTO filterSwitchingTimeDTO) {
            aCFilterSheetSpec.SwitchType = filterSwitchingTimeDTO.SwitchType;
            aCFilterSheetSpec.WorkType = filterSwitchingTimeDTO.WorkType;
            aCFilterSheetSpec.Name = filterSwitchingTimeDTO.Name;
            aCFilterSheetSpec.PhaseATimeInterval = filterSwitchingTimeDTO.PhaseATimeInterval;
            aCFilterSheetSpec.PhaseBTimeInterval = filterSwitchingTimeDTO.PhaseBTimeInterval;
            aCFilterSheetSpec.PhaseCTimeInterval = filterSwitchingTimeDTO.PhaseCTimeInterval;
            return aCFilterSheetSpec;
        }
    }
}
