using System.Reflection;

using Android.App;
using Android.OS;
using Firebase;
using Firebase.Firestore;
using Xunit.Runners.UI;
using Xunit.Sdk;

namespace GeoFire.Test.Android
{
    [Activity(Label = "GeoFire.Test.Android", MainLauncher = true)]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            FirebaseApp.InitializeApp(this);
            var firestore = FirebaseFirestore.Instance;
            var settings = new FirebaseFirestoreSettings.Builder()
                .SetTimestampsInSnapshotsEnabled(true)
                .Build();
            firestore.FirestoreSettings = settings;
            
            // tests can be inside the main assembly
            AddTestAssembly(Assembly.GetExecutingAssembly());
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
            // or in any reference assemblies
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            base.OnCreate(bundle);
        }
    }
}
