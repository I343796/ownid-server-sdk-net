using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;

namespace OwnID.Integrations.Firebase
{
    public class FirebaseContext : IFirebaseContext
    {
        public FirebaseAuth Auth { get; init;}
        
        public FirebaseApp App { get; init; }

        public FirestoreDb Db { get; init;}
    }
}