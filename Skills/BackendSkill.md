using @SPEC/DatabaseSpec.md  file plan SP creation with reference to @Backend/ScriptFlow.API\ , 
  SP should be created in Seperate file with All argument it need to perform function as intended, 
  Sp return data as intented, SP should handle
  SQL injections and user error loging if transection breaks,
  SP name should be meaning full with the naming convention added i.e  Schema +"."+ SPname
  SP should use Try catch and Error are logged in dbo.tblerrolog with Specific SP name and error stack along with insertedat updatedat, Create Dbo.tblerrorlog if not exsist
  use Proper with(nolock) to avoid locking
  Joins should have where clause where join is being made not at the end.

