using UnityEngine;

/**
 * Do the top levels of the menu item
 *
 * Version Histroy
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
        ControlOrTriangle,
        SelectControl,
        ConfirmScreen,
        Done
    };
    private UIState _uiState = UIState.Initialize;

    
    private Enums.Experiment _confirmExperiment = Enums.Experiment.None; // note: Waiting is not valid for this

    public void Start()
    {
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
                d.SetDialogElements("Choose Experiment", new string[] { "Component Experiments", "Triangle Completion Experiment", "Quit Homebase"});
                _uiState = UIState.ControlOrTriangle;
                return(Enums.Experiment.Waiting);
            case UIState.ControlOrTriangle: 
                int resp = d.GetResponse();
                switch (resp)
                {
                    case 0: 
                        d.SetDialogElements("Choose Control", new string[] { "Linear Forward Component", "Linear Backward Component", "Rotation Component", "Back" });
                        _uiState = UIState.SelectControl;
                        return(Enums.Experiment.Waiting);
                    case 1:
                        d.SetDialogElements("Confirm Choice", new string[] {"Do 'Triangle Completion Experiment'", "Back"});
                        _confirmExperiment = Enums.Experiment.TriangleCompletion;
                        _uiState = UIState.ConfirmScreen;
                        return(Enums.Experiment.Waiting);
                    case 2:
                        _uiState = UIState.Done;
                        return(Enums.Experiment.Quit);
                    case -1:
                        return(Enums.Experiment.Waiting);
                }
                Debug.Log("Not Reached (TopLevelMenu) control or triangle " + resp);
                return(Enums.Experiment.Waiting);
            case UIState.SelectControl:
                int resp2 = d.GetResponse();
                switch (resp2)
                {
                    case 0: 
                        d.SetDialogElements("Confirm Choice", new string[] {"Do Linear Forward Component Experiment", "Back"});
                        _confirmExperiment = Enums.Experiment.ControlForward;
                        _uiState = UIState.ConfirmScreen;
                        return(Enums.Experiment.Waiting);
                    case 1:
                        d.SetDialogElements("Confirm Choice", new string[] {"Do Linear Backward Component Experiment", "Back"});
                        _confirmExperiment = Enums.Experiment.ControlBackward;
                        _uiState = UIState.ConfirmScreen;
                        return(Enums.Experiment.Waiting);
                    case 2:
                        d.SetDialogElements("Confirm Choice", new string[] {"Do Rotation Component Experiment", "Back"});
                        _confirmExperiment = Enums.Experiment.ControlRotation;
                        _uiState = UIState.ConfirmScreen;
                        return(Enums.Experiment.Waiting);
                    case 3:
                     d.SetDialogElements("Choose Experiment", new string[] { "Component Experiments", "Triangle Completion Experiment", "Quit Homebase"});
                    _uiState = UIState.ControlOrTriangle;
                    return(Enums.Experiment.Waiting);
                    case -1:
                        return(Enums.Experiment.Waiting);
                }
                Debug.Log("Not Reached (TopLevelMenu) selectcontrol " + resp2);
                return(Enums.Experiment.Waiting);
            case UIState.ConfirmScreen:
                int confirm = d.GetResponse();
                switch(confirm)
                {
                    case 0:
                        _uiState = UIState.Done;
                        _dialog.SetActive(false);
                        return(_confirmExperiment);
                    case 1:
                        if(_confirmExperiment == Enums.Experiment.TriangleCompletion) {
                            d.SetDialogElements("Choose Experiment", new string[] { "Control Experiments", "Triangle Completion Experiment", "Quit Homebase"});
                            _uiState = UIState.ControlOrTriangle;
                        } 
                        else
                        {
                            d.SetDialogElements("Choose Control", new string[] { "Linear Forward Control", "Linear Backward Control", "Rotation Control" });
                            _uiState = UIState.SelectControl;
                        }
                        return(Enums.Experiment.Waiting);
                    case -1:
                        return(Enums.Experiment.Waiting);
                }
                Debug.Log("Not reached (TopLevel Menu) confirmscreen");
                return(Enums.Experiment.Waiting);
            case UIState.Done:
                return(Enums.Experiment.Waiting);
        }
        Debug.Log("Not reached (TopLevel Menu) uistate " + _uiState);
        return(Enums.Experiment.None);
    }
}
