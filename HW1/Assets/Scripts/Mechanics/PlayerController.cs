using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;


        //------------------------ OWN CODE ------------------------//
        public float distToGround = 1f;

        public bool ableToDash = true;
        public bool dashing;
        private float dashVelocity = 24f;
        private float cooldownForDash = 5f;
        private float dashTime = 0.12f;
        public Rigidbody2D rb;
        private TrailRenderer dashTrail;
        private GameObject ps;
        private ParticleSystem em;

        public bool ableToSlam = true;
        public bool ableToSlam2 = true;
        public bool slamming;
        private float slamVelocity = 15f;
        private float cooldownForSlam = 5f;
        private float slamTime = 0.12f;
        private GameObject ps2;
        private ParticleSystem em2;



        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/
        public Collider2D collider2d;
        /*internal new*/
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            dashTrail = GetComponent<TrailRenderer>();
            ps = GameObject.Find("DashParticles");
            em = ps.GetComponent<ParticleSystem>();
            ps2 = GameObject.Find("SlamParticles");
            em2 = ps2.GetComponent<ParticleSystem>();

        }

        protected override void Update()
        {

            if (slamming)
            {
                return;
            }
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump")) { 
                    Debug.Log("jumping");
                    jumpState = JumpState.PrepareToJump;
                }
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
                if (Input.GetKeyDown(KeyCode.LeftShift) && ableToDash)
                {
                    StartCoroutine(dash());
                    print("DASH!");
                }
                if (Input.GetKeyDown(KeyCode.LeftControl) && ableToSlam && ableToSlam2)
                {
                    StartCoroutine(slam());
                    print("SLAM!");
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        //------------------------ OWN CODE ------------------------//
        IEnumerator dash()
        {
            // reset dash time
            ableToDash = false;
            dashing = true;
            float grav = rb.gravityScale;
            rb.gravityScale = 0f; //so the player does not fall while dashing
            if (spriteRenderer.flipX == false)
                rb.velocity = new Vector2(transform.localScale.x * dashVelocity, 0f);
            else if (spriteRenderer.flipX == true)
                rb.velocity = new Vector2(transform.localScale.x * dashVelocity * -1, 0f);
            // trail
            dashTrail.emitting = true;
            em.Play();
            yield return new WaitForSeconds(dashTime);
            em.Stop();
            rb.velocity = new Vector2(0f, 0f);
            // Dashing is done
            dashTrail.emitting = false;
            // Player can be affected by gravity now
            rb.gravityScale = grav;
            dashing = false;
            // Start timer for cooldown
            yield return new WaitForSeconds(cooldownForDash);
            ableToDash = true;
        }

        IEnumerator slam(){
            ableToSlam = false;
            slamming = true;
            float grav = rb.gravityScale;
            //rb.gravityScale = 0f; //so the player does not fall while dashing
            rb.velocity = new Vector2(0f, transform.localScale.x * slamVelocity * -1);
            // trail
            dashTrail.emitting = true;
            yield return new WaitForSeconds(slamTime);
            rb.velocity = new Vector2(0f, 0f);
            // Slamming is done
            dashTrail.emitting = false;
            // Player can be affected by gravity now
            em2.Play();
            yield return new WaitForSeconds(0.5f);
            slamming = false;
            em2.Stop();
            // Start timer for cooldown
            yield return new WaitForSeconds(cooldownForSlam);
            ableToSlam = true;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            //float playerXonCollision = transform.position.x;
            //float playerYonCollision = transform.position.y;

            if (collision.gameObject.name.Equals("Level"))
            {
                rb.velocity = new Vector2(0f, 0f);
                ableToSlam2 = false;
            }        
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            ableToSlam2 = true;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}