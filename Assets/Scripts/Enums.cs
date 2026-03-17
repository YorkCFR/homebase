/**
 *  Possible states of the experiment
 * 
 *  Version History
 *  V3.0 - March refactoring
 *  V2.0 - February refactoring
 *  v1.1 - Dec 1, 2025. Deal with new input model
 *  V1.0 - July 27, 2020. Initial version
 * 
 *  Michael Jenkin
 **/

public static class Enums 
{
     public enum Experiment
    {
        Tutorial,
        ControlAll,
        TriangleCompletion,
        Quit,
        ControlForward,
        ControlBackward,
        ControlRotation,
        Waiting,
        None
    };
}
