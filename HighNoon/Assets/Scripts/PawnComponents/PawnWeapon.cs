using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public sealed class PawnWeapon : NetworkBehaviour
{
	private Pawn _pawn;

	private PawnInput _input;

	private PawnCameraLook _cameraLook;

	public float shootForce = 100;
	public float upwardForce = 0;
	public GameObject crosshair;
	public GameObject crosshairOuterRange;
	public Image Img_Crosshair;
	public bool CanShoot;
	public float holdingTimer = 0.5f;
	public float currentHoldingTime;
	public bool startFiring;

	[SerializeField]
	private float damage;

	[SerializeField]
	private float shotDelay;

	private float _timeUntilNextShot;

	[SerializeField]
	private Transform firePoint;

	public override void OnStartNetwork()
	{
		base.OnStartNetwork();

		_pawn = GetComponent<Pawn>();

		_input = GetComponent<PawnInput>();

		_cameraLook = GetComponent<PawnCameraLook>();
	}

	private void Update()
	{
		if (!IsOwner) return;

		if (Input.GetMouseButtonDown(0) && CanShoot)
		{
			startFiring = true;
		}
		if (Input.GetMouseButtonUp(0) && CanShoot)
		{
			startFiring = false;
			currentHoldingTime = 0;
			crosshairOuterRange.transform.localScale = Vector3.one;
			Img_Crosshair.color = Color.black;
		}

		if (Input.GetKeyDown(KeyCode.LeftCommand) && !CanShoot)
		{
			if (!GameManager.Instance.isLegalToDraw)
			{
				Debug.Log("Draw Too Soon");
			}
            
			//drawGunPanel.SetActive(true);
			//beginToDraw = true;
			//indicator.GetComponent<RectTransform>().localPosition = bottomMarker.localPosition;
		}
		ShootingTimer();

		/*
		if (_timeUntilNextShot <= 0.0f)
		{
			if (_input.fire)
			{
				ServerFire(firePoint.position, firePoint.forward,0.1f);

				_timeUntilNextShot = shotDelay;
			}
		}
		else
		{
			_timeUntilNextShot-= Time.deltaTime;
		}
		*/
	}
	
	public void ShootingTimer()
	{
		if (startFiring)
		{
			currentHoldingTime += Time.deltaTime;
			crosshairOuterRange.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f,currentHoldingTime/holdingTimer);
			Img_Crosshair.color = Color.Lerp(Color.black, Color.red, currentHoldingTime/holdingTimer);
			if (currentHoldingTime >= holdingTimer)
			{
				ServerFire(firePoint.position, firePoint.forward,0.1f);
				currentHoldingTime = 0;
				crosshairOuterRange.transform.localScale = Vector3.one;
				Img_Crosshair.color = Color.black;
			}
		}
	}
	
	private Vector3 BulletDirectionWithSpreed(float spread)
	{
		Ray ray = _cameraLook.myCamera.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
		RaycastHit hit;
        
		Vector3 targetPoint;
		
		if (Physics.Raycast(ray, out hit))
			targetPoint = hit.point;
		else
			targetPoint = ray.GetPoint(75);
        
		Vector3 directionWithoutSpread = targetPoint - firePoint.position;
        
		float x = Random.Range(-spread, spread);
		float y = Random.Range(-spread, spread);
        
		//Debug.Log("Spreed is:" + x + "," + y);
        
		return directionWithoutSpread + new Vector3(x, y, 0);
	}

	[ServerRpc]
	private void ServerFire(Vector3 firePointPosition, Vector3 firePointDirection, float spread)
	{
		GameObject bulletPrefab = Addressables.LoadAssetAsync<GameObject>("Bullet").WaitForCompletion();
		
		GameObject thisBullet = Instantiate(bulletPrefab, firePointPosition, Quaternion.identity);
        
		Vector3 bulletDirection = BulletDirectionWithSpreed(spread).normalized;
        
		thisBullet.transform.forward = bulletDirection;
        
		thisBullet.GetComponent<Rigidbody>().AddForce(bulletDirection * shootForce, ForceMode.Impulse);
		thisBullet.GetComponent<Rigidbody>().AddForce(_cameraLook.myCamera.transform.up * upwardForce, ForceMode.Impulse);
        
		ServerManager.Spawn(thisBullet);
		if (Physics.Raycast(firePointPosition, firePointDirection, out RaycastHit hit) && hit.transform.TryGetComponent(out Pawn pawn))
		{
			pawn.ReceiveDamage(damage);
		}
	}
}
