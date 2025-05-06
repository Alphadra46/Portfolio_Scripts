using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using ExtraFunctions;
using UnityEditor.Animations;

public class QTEManager : MonoBehaviour
{
    public Animator playerAnimator;
    
    //Debug
    public QTESequence tempSequence;

    [SerializeField] private GameObject inputPromptPrefab;
    [SerializeField] private Canvas overlayCanvas;
    
    //QTE Animation event
    [HideInInspector] public UnityEvent<QTEConfig.CompletionLevel> onScoreUpgrade = new UnityEvent<QTEConfig.CompletionLevel>();

    [HideInInspector] public UnityEvent onSequenceStart = new UnityEvent();
    [HideInInspector] public UnityEvent onSequenceEnd = new UnityEvent();
    [HideInInspector] public UnityEvent onQTEStart = new UnityEvent();
    [HideInInspector] public UnityEvent onQTEEnd = new UnityEvent();
    [HideInInspector] public UnityEvent onQTESuccess = new UnityEvent();
    [HideInInspector] public UnityEvent onQTEFailure = new UnityEvent();

    [HideInInspector] public QTEConfig.CompletionLevel currentQTECompletionLevel;
    [HideInInspector] public List<Enemy> targetedEnemies;
    public SO_AttackInfos currentAttackInfos;

    private QTESequence currentSequence;
    private int currentLoopIndex;
    private QTEConfig currentQTE;
    private int currentQTEIndex;
    private int correctKeyPresses = 0;
    private bool isQTEActive = false;
    private QTEKeyIcon rightInputToPress;

    private float qteStartTime;
    private GameObject qteInputPrompt;
    private Image qteInputSprite;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartSequence(tempSequence);
        }
    }

    public void StartSequence(QTESequence sequence)
    {
        currentSequence = sequence;
        currentLoopIndex = 0;
        StartQTE(currentSequence.inputSequence.First());
        onSequenceStart?.Invoke();
        
        //onQTEEnd.AddListener(ContinueSequence);
    }

    private void StartQTE(QTEConfig config)
    {
        playerAnimator.SetTrigger("StartQTE"+currentAttackInfos.attackName);
        
        currentQTE = config;
        currentQTEIndex = currentSequence.inputSequence.IndexOf(currentQTE);
        correctKeyPresses = 0;
        isQTEActive = true;
        qteStartTime = Time.time;

        KeyInit();
        
        rightInputToPress = currentQTE.possibleInputKeys.Count > 1 ? RandomSelectGoodInput(currentQTE.possibleInputKeys) : currentQTE.possibleInputKeys[0];

        InputPromptAttach(rightInputToPress.inputAction);
        qteInputPrompt = Instantiate(inputPromptPrefab, overlayCanvas.transform);
        qteInputSprite = qteInputPrompt.GetComponent<Image>();
        qteInputSprite.sprite = rightInputToPress.keyIcons[0].keyIcon; //TODO - Remplacer le 0 par l'index du device correspondant
        
        onQTEStart?.Invoke();
        Debug.LogWarning($"{rightInputToPress} QTE started !");
    }

    private IEnumerator QTETimer(float duration)
    {
        while (isQTEActive && Time.time - qteStartTime < duration)
        {
            yield return null;
        }
        
        if (isQTEActive)
        {
            EndQTE();
        }
    }
    
    private QTEKeyIcon RandomSelectGoodInput(List<QTEKeyIcon> keys)
    {
        var index = Random.Range(0, keys.Count - 1);
        return keys[index];
    }
    
    private void KeyInit()
    {
        foreach (var inputKey in currentQTE.possibleInputKeys)
        {
            inputKey.inputAction.Enable();
            inputKey.inputAction.started += OnKeyPress;
        }
    }

    private void InputPromptAttach(InputAction input)
    {
        input.started += ChangeInputPrompt;
        input.canceled += ChangeInputPrompt;
    }

    private void InputPromptDetach(InputAction input)
    {
        input.started -= ChangeInputPrompt;
        input.canceled -= ChangeInputPrompt;
    }

    private void ChangeInputPrompt(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            qteInputSprite.sprite = rightInputToPress.keyIcons[0].keyIconPressed;
        }
        else if (context.canceled)
        {
            qteInputSprite.sprite = rightInputToPress.keyIcons[0].keyIcon;
        }
    }
    
    private void OnKeyPress(InputAction.CallbackContext context)
    {
        if (!isQTEActive)
            return;
        
        if (IsGoodInputPressed(context))
        {
            switch (currentQTE.qteType)
            {
                case QTEConfig.QTEType.SpamKey:
                    correctKeyPresses++;
                    break;
                case QTEConfig.QTEType.PressKeyWithFailureOnMisinput:
                    break;
                case QTEConfig.QTEType.PressKeyWithoutFailureOnMisinput:
                    EndQTE();
                    break;
            }
        }
        else
        {
            Debug.Log("Wrong Input");
            if (currentQTE.qteType == QTEConfig.QTEType.PressKeyWithFailureOnMisinput)
            {
                //TODO - Failure + QTE Sequence End
            }
            else if (currentQTE.qteType == QTEConfig.QTEType.SpamKey && currentQTE.failDoReducedDamage)
            {
                //TODO - Réduire les dégâts
            }
        }
    }

    private bool IsGoodInputPressed(InputAction.CallbackContext context)
    {
        return context.action == rightInputToPress.inputAction;
    }
    
    public void EndQTE()
    {
        isQTEActive = false;
        onQTEEnd?.Invoke();
        InputPromptDetach(rightInputToPress.inputAction);
        Destroy(qteInputPrompt);
        
        switch (currentQTE.qteType)
        {
            case QTEConfig.QTEType.SpamKey:
                CalculateSuccessRateScore();
                break;
            case QTEConfig.QTEType.PressKeyWithFailureOnMisinput:
                break;
            case QTEConfig.QTEType.PressKeyWithoutFailureOnMisinput:
                currentSequence.sequenceCompletionLevels.Add(currentQTECompletionLevel);
                break;
        }
        
        //Désactiver les inputs du QTE qui vient de se finir
        foreach (var inputKey in currentQTE.possibleInputKeys)
        {
            inputKey.inputAction.Disable();
            inputKey.inputAction.started -= OnKeyPress;
        }

        if (currentQTE.inflictDamageOnQTEEnd)
        {
            var player = (Player_Combat)SC_TurnBasedSystem.instance.queue_units.PeekList();

            foreach (var enemy in targetedEnemies)
            {
                enemy.TakeDamage(player.finalAttack,currentAttackInfos); //TODO - Ajouter le mult du QTE
            }
        }
        
        ContinueSequence();
        
    }

    private void ContinueSequence()
    {
        bool endSequenceEarly = currentQTE.stopOnFailure && currentSequence.sequenceCompletionLevels[currentQTEIndex] == QTEConfig.CompletionLevel.Failed;
        
        if (endSequenceEarly)
        {
            EndQTESequence();
            return;
        }
        
        if (currentQTEIndex < currentSequence.inputSequence.Count - 1)
        {
            StartQTE(currentSequence.inputSequence[currentQTEIndex + 1]); //TODO - Mettre un delais ou un trigger/event qui permet de déclencher le QTE suviant plus tard/ en fonction de l'animation
        }
        else
        {
            if (currentSequence.loopSequence && currentLoopIndex <= currentSequence.loopAmount-1)
            {
                currentQTEIndex = 0;
                StartQTE(currentSequence.inputSequence[0]);
                currentLoopIndex ++;
            }
            else
            {
                EndQTESequence();
            }
        }
    }

    private void EndQTESequence()
    {
        if (currentSequence.inflictDamageOnSequenceEnd)
        {
            var player = (Player_Combat)SC_TurnBasedSystem.instance.queue_units.PeekList();

            foreach (var enemy in targetedEnemies)
            {
                enemy.TakeDamage(player.finalAttack,currentAttackInfos); //TODO - Ajouter le mult du QTE
            }
        }
        
        targetedEnemies.Clear();
        SC_TurnBasedSystem.instance.EndTurn(); //TODO - Enlever l'UI des actions du joueur - Terminer le tour uniquement après la prise de dégât de l'ennemi + Move cette ligne à la fin de la sequence de QTE
        currentSequence.sequenceCompletionLevels.Clear();
        onSequenceEnd?.Invoke();
    }
    
    /// <summary>
    /// Calcule le taux de succès pour un QTE de type SpamKey
    /// </summary>
    private void CalculateSuccessRateScore()
    {
        float successRate = (float)correctKeyPresses / currentQTE.requiredKeyPresses;
        QTEConfig.CompletionLevel completionLevel = successRate < currentQTE.okThreshold
            ? QTEConfig.CompletionLevel.Failed
            :successRate >= currentQTE.okThreshold && successRate < currentQTE.goodThreshold
                ?
                QTEConfig.CompletionLevel.Ok
                : successRate >= currentQTE.goodThreshold && successRate < currentQTE.greatThreshold
                    ? 
                    QTEConfig.CompletionLevel.Good
                    : successRate >= currentQTE.greatThreshold && successRate < currentQTE.perfectThreshold
                        ? 
                        QTEConfig.CompletionLevel.Great
                        : QTEConfig.CompletionLevel.Perfect;
        
        //TODO - Mettre la partie en dessous dans le script attaché à l'objet animé (QTEAnimationEvent)
        
        if (completionLevel == QTEConfig.CompletionLevel.Failed)
        {
            onQTEFailure?.Invoke();
        }
        else
        {
            currentSequence.sequenceCompletionLevels.Add(completionLevel);
            //onQTESuccess?.Invoke(completionLevel);
        }
        
        
    }
}
