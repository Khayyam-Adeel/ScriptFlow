use plan mode to build api

Plan API building and coding walk me thorugh each step of planing and implementation, code should be humanily understandable and use @Spec/ApiSpec.md File to plan and organize this using clean code artitacture. No further
changes are required wihput my approval , changes should be scopped only to Scriptflow.api project


database design based on api dtos

Based on ScriptFlow.Api/DTOs create me a database design specification document that include table structure with isactive, isdeleted, insertedat, updatedat,insertedby, updatedby column along with DTO defined columns,
  document must includde SQL querries for creation of tables and primary keys defined in table, along with constraints if needed. save document in DatabaseSpec.md file in the project.

dispatch worker 
 by using @Backend/Dispatch.Worker/Spec.md please plan dispatch service , add a implementation plan in the same directory after reviewing i will ask for implementation .use Scriptflow.api for reference, code should be
  humanily understanable and with proper comments on each line to tell me what excatly is happening in code.


Database SP creation PROMPT

using @SPEC/DatabaseSpec.md  file plan SP creation with reference to @Backend/ScriptFlow.API\ , 
  SP should be created in Seperate file with All argument it need to perform function as intended, 
  Sp return data as intented, SP should handle
  SQL injections and user error loging if transection breaks,
  SP name should be meaning full with the naming convention added i.e  Schema +"."+ SPname
  SP should use Try catch and Error are logged in dbo.tblerrolog with Specific SP name and error stack along with insertedat updatedat, Create Dbo.tblerrorlog if not exsist