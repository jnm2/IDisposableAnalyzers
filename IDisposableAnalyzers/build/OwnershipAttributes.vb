Namespace IDisposableAnalyzers
    Imports System

    ''' <summary>
    ''' The return value must be disposed by the caller.
    ''' </summary>
    <AttributeUsage(AttributeTargets.ReturnValue Or AttributeTargets.Parameter, AllowMultiple:=False, Inherited:=True)>
    Friend NotInheritable Class GivesOwnershipAttribute
        Inherits Attribute
    End Class

    ''' <summary>
    ''' The return value must not be disposed by the caller.
    ''' </summary>
    <AttributeUsage(AttributeTargets.ReturnValue Or AttributeTargets.Parameter, AllowMultiple:=False, Inherited:=True)>
    Friend NotInheritable Class KeepsOwnershipAttribute
        Inherits Attribute
    End Class

    ''' <summary>
    ''' The ownership of instance is transferred and the receiver is responsible for disposing.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False, Inherited:=True)>
    Friend NotInheritable Class TakesOwnershipAttribute
        Inherits Attribute
    End Class
End Namespace
