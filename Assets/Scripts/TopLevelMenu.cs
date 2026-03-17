using UnityEngine;

/**
 * Do the top levels of the menu item
 *
 * Version Histroy
 * V3.0 - March refactoring
 * V2.0 - February refactoring
 * V1.1 - minor bug tweaks and better 'back' performance
 * V1.0 - refactoring of the initial system.
 *
 * Note that many classes are attached to the same parent object, simplifying some of
 * the effort here.
 *
 * Michael Jenkin, 2026
 **/

public class TopLevelMenu : MonoBehaviour
{
     private enum UIState
    {
        Initialize,
        TopLevel,
    };
    private UIState _uiState = UIState.Initialize;
    private InputHandler _inputHandler;



    public void Start()
    {
        GameObject camera = GameObject.Find("Camera Holder");
        _inputHandler = camera.GetComponent<InputHandler>();
    }

    public void Reset()
    {
        _uiState = UIState.Initialize;
    }
 
    public Enums.Experiment DealWithMenu()
    {
        HomeBaseDriver driver = GetComponent<HomeBaseDriver>();
        GameObject _dialog = driver.Dialog;
        Dialog d = _dialog.GetComponent<Dialog>();
 
        switch (_uiState)
        {
            case UIState.Initialize:  // bring up the choose experiment screen
                _dialog.SetActive(true);
                _inputHandler.UseHorizontalAxis();
                d.SetDialogElements("Select Task", new string[] { "Do Tutorial", "Component Tasks", "Triangle Completion Task", "Quit Homebase"});
                _uiState = UIState.TopLevel;
                return(Enums.Experiment.Waiting);
            case UIState.TopLevel: 
                int resp = d.GetResponse();
                switch (resp)
                {
                    case 0: 
                        _dialog.SetActive(false);
                        return(Enums.Experiment.Tutorial);
                    case 1:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.ControlAll);
                    case 2:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.TriangleCompletion);
                    case 3:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.Quit);
/* Keep these in case ever needed again
                    case 4:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.ControlForward);
                    case 5:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.ControlBackward);
                    case 6:
                        _dialog.SetActive(false);
                        return(Enums.Experiment.ControlRotation);
*/
                    case -1:
                        return(Enums.Experiment.Waiting);
                }
                return(Enums.Experiment.Waiting);
            default:
                return(Enums.Experiment.Waiting);
        }
    }
}
