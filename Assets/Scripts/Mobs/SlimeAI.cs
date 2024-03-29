﻿using UnityEngine;
using System.Collections;

/// <summary>
/// El Slime decide cada 3 segundos si moverse o esperar.
/// Cuando se mueve, se mueve durante 1 segundo en una dirección aleatoria
/// y luego espera al siguiente ciclo.
/// </summary>
public class SlimeAI : MonoBehaviour 
{
	// Tiempo en segundos entre las acciones
	public float m_cooldown = 3f;
	private float m_defaultCooldown;

	public float m_jumpCooldown = 1f;
	private float m_defaultJumpCooldown;


	public GLOBALS.Action m_action = GLOBALS.Action.Wait;

	// Destino elegido para el salto
	public GLOBALS.Direction m_jumpDirection = GLOBALS.Direction.North;

	private Movement m_movement;

	private MobState m_mobState;

	private Animator m_animator;


	void Start () 
	{
		m_defaultCooldown = m_cooldown;
		m_defaultJumpCooldown = m_jumpCooldown;
		m_movement = GetComponent<Movement>();
		m_mobState = GetComponent<MobState>();
		m_animator = GetComponent<Animator>();
	}
	

	void Update () 
	{
		if (Clock())
		{
			// Si nos han dado, no calculamos otro movimiento, 
			// esperamos siguiente turno
			if (!m_mobState.m_beenHit && !m_mobState.m_dead)
			{
				DecideAction();
			}
			else
			{
				m_action = GLOBALS.Action.Wait;
			}

		}

		if (m_action == GLOBALS.Action.Jump)
		{
			if (JumpClock())
			{
				m_action = GLOBALS.Action.Wait;
				m_animator.SetBool("jump", false);
			}
			else
			{
				Jump(m_jumpDirection);
			}
		}
	}


	private bool Clock()
	{
		m_cooldown -= Time.deltaTime;
		if (m_cooldown <= 0f)
		{
			m_cooldown = m_defaultCooldown;
			return true;
		}
		else
		{
			return false;
		}
	}


	private bool JumpClock()
	{
		m_jumpCooldown -= Time.deltaTime;
		if (m_jumpCooldown <= 0f)
		{
			m_jumpCooldown = m_defaultJumpCooldown;
			return true;
		}
		else
		{
			return false;
		}
	}


	private void DecideAction ()
	{
		int randomAction = Random.Range(0,2);
		switch (randomAction)
		{
		case 0:
			m_action = GLOBALS.Action.Wait;
			break;
		case 1:
			m_action = GLOBALS.Action.Jump;
			m_animator.SetBool("jump", true);
			break;
		default:
			m_action = GLOBALS.Action.Wait;
			break;
		}

		if (m_action == GLOBALS.Action.Jump)
		{
			m_jumpDirection = DecideDirection();
		}
	}


	private GLOBALS.Direction DecideDirection ()
	{
		int randomDir = Random.Range(0,4);

		GLOBALS.Direction direction;

		switch (randomDir)
		{
		case 0:
			direction = GLOBALS.Direction.North;
			break;
		case 1:
			direction = GLOBALS.Direction.East;
			break;
		case 2:
			direction = GLOBALS.Direction.South;
			break;
		case 3:
			direction = GLOBALS.Direction.West;
			break;
		default:
			direction = GLOBALS.Direction.North;
			break;
		}

		return direction;
	}


	private void Jump ( GLOBALS.Direction p_direction )
	{
		Vector2 axisValue = Vector2.zero;

		switch (p_direction)
		{
		case GLOBALS.Direction.North:
			axisValue = new Vector2 (0f,1f);
			break;
		case GLOBALS.Direction.East:
			axisValue = new Vector2 (1f,0f);
			break;
		case GLOBALS.Direction.South:
			axisValue = new Vector2 (0f,-1f);
			break;
		case GLOBALS.Direction.West:
			axisValue = new Vector2 (-1f,0f);
			break;
		}

		m_movement.Move(axisValue);
	}
}
