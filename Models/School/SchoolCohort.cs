using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    [Obsolete("Use Cohort instead. This class is kept for compatibility and inherits from Cohort.")]
    public class SchoolCohort : Cohort
    {
        // Intentionally empty - inherits everything from Cohort
    }
}
