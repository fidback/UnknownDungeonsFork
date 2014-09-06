using UnityEngine;
using System.Collections;

/// <summary>
/// Script encargado de calcular toda la logica del jugador.
/// Responde a las llamadas del script FilipInput y gestiona y actualiza
/// los estados del jugador (vida, municion, si esta en el aire...)
/// </summary>
public class FilipState : MonoBehaviour 
{
	#region Instancia Singleton
	/// <summary>
	/// Referencia estatica a la instancia Singleton del jugador.
	/// Para que la GUI pueda mostrar informacion relacionada con su estado
	/// </summary>
	public static FilipState myFilip = null;
	#endregion


	#region Atributos del jugador
	// Si Filip se debe mover o no (debug de animaciones)
	public bool m_move = true;

	/// Vida maxima
	public int m_fullHP = 5;
	/// Vida actual
	public int m_hp = 3;

	/// Dinero de Filip
	public int m_coins = 0;

	/// Numero de armas
	private const int m_numberOfWeapons = 3;
	
	/// Arma equipada por defecto (0-espada, 1-arco, 2-magia)
	[Range(0, m_numberOfWeapons -1)]
	public int m_equippedWeapon = 0;
	#endregion

	
	#region Referencias a prefabs de armas y sus atributos
	/// Prefab de la flecha
	public Transform m_arrow;
	/// Offset de posicion al instanciar
	public float m_arrowStart = 10f;
	/// Nivel de hablidad con flechas
	public int m_arrowLvl = 1;

	/// Prefab del espadazo
	public Transform m_swordSwing;
	/// Offset de posicion al instanciar
	public float m_swordRange = 18f;
	/// Nivel de hablidad con espada
	public int m_swordLvl = 1;

	/// Prefab de la magia
	public Transform m_magicBall;
	/// Offset de posicion al instanciar
	public float m_magicBallRange = 24f;
	/// Nivel de hablidad con magia
	public int m_magicLvl = 1;

	/// Nivel de escudo
	public int m_shieldLvl = 1;
	#endregion


	#region Estados de Filip
	/// Direccion a la que mira
	public int m_facingDirection = 2;

	/// Esta atacando?
	public bool m_attacking = false;
	/// Cooldown entre disparos
	public float m_attackCooldown = 1f;
	/// Cooldown entre disparos (para inicializar)
	private float m_attackDefaultCooldown;

	/// Esta defendiendo?
	public bool m_defending = false;

	/// Le han dado al jugador?
	public bool m_beenHit = false;
	/// Cooldown entre hits
	public float m_beenHitCooldown = 1f;
	/// Cooldown entre hits (para inicializar)
	private float m_beenHitDefaultCooldown;

	/// Se puede mover el jugador?
	public bool m_canMove = true;
	/// Cooldown de la inmovilidad
	public float m_canMoveCooldown = 0.1f;
	/// Cooldown de la inmovilidad (para inicializar)
	private float m_canMoveDefaultCooldown;

	/// Esta muerto?
	public bool m_dead = false;
	#endregion


	#region Referencias para otros componentes del actor
	/// Referencia al animator para desencadenar las animaciones
	private Animator m_animator;

	/// Referencia al script de movimiento pixelado
	private Movement m_movement;
	#endregion


	#region MonoBehaviour Messages
	void Awake () 
	{
		#region Tratamiento Singleton
		// Si no existe
		if (myFilip == null)
		{
			// Marcamos el objeto para no ser destruido al cambiar de escena
			DontDestroyOnLoad(transform.gameObject);
			// Asignamos esta instancia a la instancia unica Singleton
			myFilip = this;
		}
		// Si ya existia
		else if (myFilip != this)
		{
			// Destruimos este objeto
			Destroy(gameObject);
		}
		#endregion

		// Almacenamos los CDs por si se han ajustado en el editor
		m_attackDefaultCooldown = m_attackCooldown;
		m_beenHitDefaultCooldown = m_beenHitCooldown;
		m_canMoveDefaultCooldown = m_canMoveCooldown;

		// Recogemos las referencias de los componentes que necesitamos
		m_animator = GetComponentInChildren<Animator>();
		m_movement = GetComponent<Movement>();
	}
	

	void Update () 
	{
		if (m_dead)
		{
			Destroy(gameObject, 1);
		}

		// Mientras el estado attacking este activado, el jugador no atacara mas
		if (m_attacking)
		{
			// Cuando el cooldown se acaba, desactivamos el estado Atacando
			// Filip ya puede volver a atacar
			if (AttackClock())
			{
				m_attacking = false;
			}
		}

		// Mientras el estado hit este activado, el jugador sera invulnerable
		if (m_beenHit)
		{
			// Cuando el cooldown se acaba, desactivamos el estado Alcanzado
			// Filip puede volver a ser dañado
			if (BeenHitClock())
			{
				m_beenHit = false;
			}
		}

		// Cuando canMove se desactiva, el jugador no se puede mover
		if (!m_canMove)
		{
			// Cuando el cooldown se acaba, activamos canMove
			// Filip ya se puede mover
			if (CanMoveClock())
			{
				m_canMove = true;
			}
		}
	}


	void OnCollisionStay2D (Collision2D p_collision)
	{
		// Si nos toca algo con la etiqueta enemigo, nos hace daño
		if (p_collision.gameObject.tag == "Enemy")
		{
			ApplyDamage(1);
		}
	}
	
	
	void OnTriggerEnter2D (Collider2D p_collider)
	{
		// Si entramos en una puerta
		if (p_collider.gameObject.tag == "Door")
		{
			// Recogemos el script de la puerta
			Door door = p_collider.gameObject.GetComponent<Door>();
			// Para obtener la escena que queremos cargar
			GameState.LoadScene(door.m_targetScene, door.m_doorDirection);
		}

		// Si es un item, e.g. una moneda
		if (p_collider.gameObject.tag == "Pickable")
		{
			myFilip.m_coins += 1;
			Destroy(p_collider.gameObject);
			SoundHelper.PlayPickCoin();
		}
	}
	#endregion


	#region Metodos de la clase
	/// <summary>
	/// El personaje anda en la direccion del parametro input
	/// </summary>
	/// <param name="p_inputAxis">Vector2 con el valor del movimiento horizontal a realizar</param>
	public void Walk ( Vector2 p_inputAxis )
	{
		// En el animador usamos el booleano walking para saber si estamos andando
		// y el integer direction para saber en que direccion
		if (p_inputAxis.x > 0)
		{
			m_animator.SetBool("andando", true);
			m_animator.SetInteger("direccion", 1);
			FaceDirection(GLOBALS.Direction.East);
		}
		if (p_inputAxis.x < 0)
		{
			m_animator.SetBool("andando", true);
			m_animator.SetInteger("direccion", 3);
			FaceDirection(GLOBALS.Direction.West);
		}
		if (p_inputAxis.y > 0)
		{
			m_animator.SetBool("andando", true);
			m_animator.SetInteger("direccion", 0);
			FaceDirection(GLOBALS.Direction.North);
		}
		if (p_inputAxis.y < 0)
		{
			m_animator.SetBool("andando", true);
			m_animator.SetInteger("direccion", 2);
			FaceDirection(GLOBALS.Direction.South);
		}
		if (p_inputAxis.x == 0 && p_inputAxis.y == 0)
		{
			m_animator.SetBool("andando", false);
		}

		if (m_move && m_canMove && !m_defending)
		{
			Vector3 movement = new Vector3(
				p_inputAxis.x,
				p_inputAxis.y,
				0f);
			m_movement.Move(movement);
		}
	}


	/// <summary>
	/// Ataca en la direccion que este mirando el jugador.
	/// </summary>
	public void Attack ()
	{
		if (!m_attacking && m_canMove && !m_defending)
		{
			m_attacking = true;
			m_canMove = false;

			// Primero miramos que arma esta equipada
			switch (m_equippedWeapon)
			{

			case 0: // Espada
				// Calculamos la rotacion y posicion de salida
				int swordRotation = GetProjectileRotation(m_facingDirection);
				Vector3 swordPosition = GetProjectilePosition(
					m_facingDirection,
					m_swordRange);

				// Instanciamos
				Transform sword = Instantiate(
					m_swordSwing,
					swordPosition,
					this.transform.rotation) as Transform ;

				// Rotamos el objeto despues
				sword.transform.Rotate(Vector3.forward * swordRotation);

				// Reproducir el sonido de la espada
				SoundHelper.PlaySwordSwing();
				break;

			case 1: // Arco
				// Calculamos la rotacion y posicion de salida
				int arrowRotation = GetProjectileRotation(m_facingDirection);
				Vector3 arrowPosition = GetProjectilePosition(
					m_facingDirection,
					m_arrowStart);

				// Instanciamos
				Transform arrow = Instantiate(
					m_arrow,
					arrowPosition,
					this.transform.rotation) as Transform ;

				// Rotamos el objeto despues
				arrow.transform.Rotate(Vector3.forward * arrowRotation);

				// Reproducir sonido flecha
				SoundHelper.PlayArrowShot();
				break;

			case 2: // Magia
				// Calculamos la rotacion y posicion de salida
				int magicRotation = GetProjectileRotation(m_facingDirection);
				Vector3 magicPosition = GetProjectilePosition(
					m_facingDirection,
					m_magicBallRange);

				// Instanciamos
				Transform magic = Instantiate(
					m_magicBall,
					magicPosition,
					this.transform.rotation) as Transform ;

				// Rotamos el objeto despues
				magic.transform.Rotate(Vector3.forward * magicRotation);

				// Reproducir sonido bola magica
				SoundHelper.PlayMagicBall();
				break;

			default:
				break;
			}

			// Animacion del ataque
			m_animator.SetTrigger("ataque");
		}
	}


	/// <summary>
	/// Sacar el escudo.
	/// Filip es invulnerable mientras se cubra con el.
	/// </summary>
	public void Defend ()
	{
		if (!m_defending && m_canMove)
		{
			m_defending = true;
			m_animator.SetBool("defendiendo", true);
		}
	}
	

	/// <summary>
	/// Guarda el escudo.
	/// </summary>
	public void StopDefend ()
	{
		if (m_defending)
		{
			m_defending = false;
			m_animator.SetBool("defendiendo", false);
		}
	}
	

	/// <summary>
	/// Cambiar de arma.
	/// Equipa la siguiente arma de la lista (Espada - Flecha - Magia).
	/// </summary>
	public void ChangeWeapon ()
	{
		m_equippedWeapon += 1;
		if (m_equippedWeapon >= m_numberOfWeapons)
		{
			m_equippedWeapon = 0;
		}
		m_animator.SetInteger("arma", m_equippedWeapon);
	}


	/// <summary>
	/// Actualiza el estado m_facingDirection del jugador,
	/// que indica hacia que direccion esta mirando.
	/// </summary>
	/// <param name="p_direction">P_direction.</param>
	public void FaceDirection(GLOBALS.Direction p_direction)
	{
		switch (p_direction)
		{
		case GLOBALS.Direction.North:
			m_facingDirection = 0;
			break;
		case GLOBALS.Direction.East:
			m_facingDirection = 1;
			break;
		case GLOBALS.Direction.South:
			m_facingDirection = 2;
			break;
		case GLOBALS.Direction.West:
			m_facingDirection = 3;
			break;
		default:
			m_facingDirection = 0;
			break;
		}
	}

	
	/// <summary>
	/// Calcula la rotacion que debe tener el proyectil al instanciarse,
	/// segun la direccion en la que esta apuntando el jugador.
	/// </summary>
	/// <returns> Devuelve la rotacion en grados para el proyectil.</returns>
	/// <param name="p_direction"> Direccion a la que apunta el jugador.</param>
	private int GetProjectileRotation ( int p_direction )
	{
		int rotation = 0;
		switch (p_direction)
		{
		case 0:
			rotation = 0;
			break;
		case 1:
			rotation = -90;
			break;
		case 2:
			rotation = 180;
			break;
		case 3:
			rotation = 90;
			break;
		default:
			rotation = 0;
			break;
		}
		
		return rotation;
	}

	
	/// <summary>
	/// Obtenemos la posicion para el proyectil a instanciar
	/// </summary>
	/// <returns>La posicion para el proyectil</returns>
	/// <param name="p_direction">La direccion en la que apunta el jugador</param>
	/// <param name="p_range">La distancia a la que debe instanciarse el proyectil</param>
	private Vector3 GetProjectilePosition ( int p_direction, float p_range )
	{
		Vector2 position = new Vector2(0f,0f);
		switch (p_direction)
		{
		case 0:
			position.y = 1 * p_range;
			position.x = 0f * GLOBALS.UNITS_TO_PIXELS;
			position.y *= GLOBALS.UNITS_TO_PIXELS;
			break;
		case 1:
			position.x = 1 * p_range;
			position.x *= GLOBALS.UNITS_TO_PIXELS;
			position.y = -6f * GLOBALS.UNITS_TO_PIXELS;
			break;
		case 2:
			position.y = -1 * p_range;
			position.x = 0f * GLOBALS.UNITS_TO_PIXELS;
			position.y *= GLOBALS.UNITS_TO_PIXELS;
			break;
		case 3:
			position.x = -1 * p_range;
			position.x *= GLOBALS.UNITS_TO_PIXELS;
			position.y = -6f * GLOBALS.UNITS_TO_PIXELS;
			break;
		default:
			break;
		}
		
		return new Vector3(
			this.transform.position.x + position.x,
			this.transform.position.y + position.y,
			0f);
	}


	/// <summary>
	/// Decrementa el cooldown del estado !canMove.
	/// reinicia el cooldown cuando se acaba.
	/// </summary>
	/// <returns><c>true</c>, si el cooldown se ha acabado, <c>false</c> si no.</returns>
	private bool CanMoveClock ()
	{
		m_canMoveCooldown -= Time.deltaTime;
		if (m_canMoveCooldown <= 0f)
		{
			m_canMoveCooldown = m_canMoveDefaultCooldown;
			return true;
		}
		else
		{
			return false;
		}
	}


	/// <summary>
	/// Decrementa el cooldown del estado Attacking.
	/// reinicia el cooldown cuando se acaba.
	/// </summary>
	/// <returns><c>true</c>, si el cooldown se ha acabado, <c>false</c> si no.</returns>
	private bool AttackClock ()
	{
		m_attackCooldown -= Time.deltaTime;
		if (m_attackCooldown <= 0f)
		{
			m_attackCooldown = m_attackDefaultCooldown;
			return true;
		}
		else
		{
			return false;
		}
	}


	/// <summary>
	/// Decrementa el cooldown del estado beenHit.
	/// reinicia el cooldown cuando se acaba.
	/// </summary>
	/// <returns><c>true</c>, si el cooldown se ha acabado, <c>false</c> si no.</returns>
	private bool BeenHitClock ()
	{
		m_beenHitCooldown -= Time.deltaTime;
		if (m_beenHitCooldown <= 0f)
		{
			m_beenHitCooldown = m_beenHitDefaultCooldown;
			return true;
		}
		else
		{
			return false;
		}
	}


	/// <summary>
	/// Aplica el daño del parametro al jugador
	/// </summary>
	/// <param name="p_damage">Daño a aplicar.</param>
	public void ApplyDamage ( int p_damage )
	{
		if (!m_beenHit && !m_defending)
		{
			m_hp -= p_damage;
			m_beenHit = true;
			m_canMove = false;

			// Animamos el parpadeo de Filip
			m_animator.SetTrigger("herido");
			
			// Reproducir sonido herido
			SoundHelper.PlayPlayerHit();

			// Si se nos acaba la vida
			if (m_hp <= 0)
			{
				m_dead = true;
				GameState.PlayerDead();
			}
		}
	}
	#endregion
}
