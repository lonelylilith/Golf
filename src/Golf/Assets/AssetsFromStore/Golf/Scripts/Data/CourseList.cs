using UnityEngine;

namespace Golf
{
    [CreateAssetMenu(fileName = "Course List", menuName = "New Course List")]
    public class CourseList : ScriptableObject
    {
        public CourseData[] Courses;
    }
}