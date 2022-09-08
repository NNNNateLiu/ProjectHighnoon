using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using FishNet.Component.Animating;

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
	
	[Header("Bullets")]
	public int bulletCount = 6;
	public List<GameObject> bulletsInCylinder;
	public int currentBulletIndex = 0;
	public bool startReloading;

	[Header("Indicator")]
	public GameObject movingPanel;
	public GameObject indicator;
	public RectTransform bottomMarker;
	public RectTransform upperMarker;
	public float indicatorRunningSpeed;
	public bool beginToDraw;
	
	
	
	public NetworkAnimator networkRevolver;

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
		
		if (Input.GetKeyDown(KeyCode.R) && CanShoot)
		{
			movingPanel.SetActive(true);
			startReloading = true;
			indicator.GetComponent<RectTransform>().localPosition = bottomMarker.localPosition;
		}
		if (Input.GetKeyUp(KeyCode.R) && CanShoot)
		{
			movingPanel.SetActive(false);
			beginToDraw = false;
			CheckIfReloadSuccessfully();
		}

		if (Input.GetKeyDown(KeyCode.LeftControl) && !CanShoot)
		{
			if (!GameManager.Instance.isLegalToDraw)
			{
				Debug.Log("Draw Too Soon");
				return;
			}
			movingPanel.SetActive(true);
			beginToDraw = true;
			indicator.GetComponent<RectTransform>().localPosition = bottomMarker.localPosition;
		}
		if (Input.GetKeyUp(KeyCode.LeftControl) && !CanShoot)
		{
			movingPanel.SetActive(false);
			beginToDraw = false;
			CheckIfDrawSuccessfully();
		}
		
		ShootingTimer();
		MovingPanelCheck();

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
				if (bulletCount == 0)
				{
					return;
				}
				ServerFire(firePoint.position, firePoint.forward,0.1f);
				networkRevolver.SetTrigger("AimFire");
				currentHoldingTime = 0;
				crosshairOuterRange.transform.localScale = Vector3.one;
				Img_Crosshair.color = Color.black;
				consumingBullet();
			}
		}
	}
	
	public void consumingBullet()
	{
		bulletCount--;
		bulletsInCylinder[currentBulletIndex].SetActive(false);
		currentBulletIndex++;
	}

	public void reload()
	{
		bulletCount = 6;
		foreach (var bulletIcon in bulletsInCylinder)
		{
			bulletIcon.SetActive(true);
		}
		currentBulletIndex = 0;
	}

	public void MovingPanelCheck()
	{
		if (beginToDraw || startReloading)
		{
			Vector3 tempPos = indicator.GetComponent<RectTransform>().localPosition;
			if (tempPos.y > upperMarker.localPosition.y || tempPos.y < bottomMarker.localPosition.y)
			{
				indicatorRunningSpeed *= -1;
			}
			tempPos.y += indicatorRunningSpeed;
			indicator.GetComponent<RectTransform>().localPosition = tempPos;
		}
	}
	
	public void CheckIfDrawSuccessfully()
	{
		if (indicator.GetComponent<RectTransform>().localPosition.y <= 56 &&
		    indicator.GetComponent<RectTransform>().localPosition.y >= 36)
		{
			CanShoot = true;
			crosshair.SetActive(true);
			networkRevolver.SetTrigger("DrawGun");
		}
	}
	
	public void CheckIfReloadSuccessfully()
	{
		if (indicator.GetComponent<RectTransform>().localPosition.y <= 56 &&
		    indicator.GetComponent<RectTransform>().localPosition.y >= 36)
		{
			reload();
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
		GameObject gunSmokePrefab = Addressables.LoadAssetAsync<GameObject>("GunSmoke").WaitForCompletion();
		
		GameObject thisBullet = Instantiate(bulletPrefab, firePointPosition, Quaternion.identity);
		GameObject thisBulletGunSmoke = Instantiate(gunSmokePrefab, firePointPosition, Quaternion.identity);
        
		Vector3 bulletDirection = BulletDirectionWithSpreed(spread).normalized;
        
		thisBullet.transform.forward = bulletDirection;
        
		thisBullet.GetComponent<Rigidbody>().AddForce(bulletDirection * shootForce, ForceMode.Impulse);
		thisBullet.GetComponent<Rigidbody>().AddForce(_cameraLook.myCamera.transform.up * upwardForce, ForceMode.Impulse);
        
		ServerManager.Spawn(thisBulletGunSmoke);
		ServerManager.Spawn(thisBullet);
		
		if (Physics.Raycast(firePointPosition, firePointDirection, out RaycastHit hit) && hit.transform.TryGetComponent(out Pawn pawn))
		{
			GameObject bloodPrefab = Addressables.LoadAssetAsync<GameObject>("Blood").WaitForCompletion();
			
			GameObject thisBlood = Instantiate(bloodPrefab, hit.point, Quaternion.identity);
			
			ServerManager.Spawn(thisBlood);
			
			pawn.ReceiveDamage(damage);
		}
	}
}
