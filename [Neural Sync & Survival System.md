## **\[Neural Sync & Survival System 상세 설계서\]**

### **1\. 스크립트 변경 및 생성 목록**

| 구분 | 스크립트 이름 | 주요 변경 내용 |
| :---- | :---- | :---- |
| **신규 생성** | GlobalSettingsSO.cs | 시스템 전반의 임계값(Threshold) 및 배율(Multiplier)을 관리하는 데이터 파일. |
| **데이터 수정** | UnitDataSO.cs | 클래스별 기본 생존 확률 및 뉴럴 싱크 기본값 필드 추가. 1 |
| **인터페이스 수정** | IDurationEffect.cs | 개별 상태이상이 생존 확률에 주는 패널티 속성 추가. 2 |
| **로직 수정** | UnitStatus.cs | 뉴럴 싱크 관리, 상태 갱신, 최종 생존 확률 계산 로직 구현. 3 |
| **참조 활용** | QTEManager.cs | QTE 성공 여부 판단 (기존 코드 활용). 4 |

---

### **2\. 고쳐야 할 스크립트 및 메서드 상세**

#### **A.**

UnitStatus.cs 5

* **추가 필드:**  
  1. float currentSync: 현재 신경 동기화율 (0\~200).  
  2. bool hasOverclockTriggered: 오버클럭 주사위 실행 여부 (게임 중 유닛당 1회).  
  3. bool isSystemLocked: 시스템 에러로 인한 잠금 상태 여부 (100까지 복구 필요).  
* **수정/추가 메서드:**  
  1. **UpdateSync(float amount)**: 싱크로율 수치를 가감하고, 50 미만 시 크라이시스 체크를 수행함.  
  2. **HandleCrisisCheck()**: 싱크 50 미만 첫 진입 시 오버클럭 주사위(5%)를 굴림.  
  3. **UpdateConditionFromSync()**: 현재 싱크로율 및 isSystemLocked 상태에 따라 UnitCondition을 강제 갱신함. 6  
  4. **CalculateSurvivalChance()**: 확정된 공식을 사용하여 최종 확률(float)을 반환함.  
  5. **CheckSurvival()**: TakeDamage 시 호출되며, 계산된 확률과 QTE 성공 여부를 대조함. 7  
  6. **TakeDamage(int amount, bool isMyTurn, bool isCrit)**: HP가 0 이하일 때만 CheckSurvival을 호출하도록 유지. 8

#### **B.**

UnitDataSO.cs 9

* **BaseStatsStruct 내 추가:** 10  
  * float BaseSurvivalChance: 클래스별 기본 확률 (Assault: 5, Scout: 8, Sniper: 2).  
  * float BaseNeuralSync: 초기 싱크로율 (기본 100).  
  * float BaseOverclockChance: 오버클럭 성공 확률 (기본 5%).

#### **C.**

IDurationEffect.cs 11

* **추가 프로퍼티:**  
  * float SurvivalPenalty { get; }: 생존 확률에 대한 곱연산 패널티 값 (예: 화상 시 0.1 부여 시 0.9배 적용).

---

### **3\. 신규 스크립트 상세 (GlobalSettingsSO)**

* **역할:** 기획자가 코드를 건드리지 않고 인스펙터에서 모든 밸런스 수치를 조정할 수 있게 함.  
* **주요 필드:**  
  * Threshold\_Hopeful (180), Threshold\_Inspired (150)  
  * Overclock\_Value (160): 성공 시 설정될 수치.  
  * Mult\_Hopeful (1.5), Mult\_Inspired (1.2), Mult\_Normal (1.0), Mult\_Incapacitated (0.8), Mult\_Fleeing (0.5), Mult\_FriendlyFire (0.2).  
  * Burn\_Penalty (0.1): 화상 상태이상 기본 패널티.

---

### **4\. 메서드 간 상호작용 및 인터페이스**

| 호출 메서드 | 호출 대상 메서드 | 인자 (Type) | 반환값 (Type) | 비고 |
| :---- | :---- | :---- | :---- | :---- |
| UpdateSync | HandleCrisisCheck | 없음 | void | 50 미만 진입 시 1회 호출 |
| CalculateSurvivalChance | StatusEffectController.ActiveEffects | 없음 | List\<IDurationEffect\> | 디버프 패널티 합산용 12 |
| CheckSurvival | CalculateSurvivalChance | 없음 | float | 최종 확률 획득 |
| CheckSurvival | QTEManager.GetQTESuccess | 없음 | bool | QTE 결과 확인 13 |
| UnitStatus | UpdateSync | float amount | void | 타 유닛 사망, 피격 등에서 호출 |

---

### **5\. 최종 확정 공식 (Survival Probability)**

생존 확률은 \*\*Sync 5 미만일 경우 무조건 0%\*\*이며, 5 이상일 경우 아래 공식을 따릅니다.

#### **단계 1: 싱크로율 기반 기초 확률 ($P\_{Sync}$)**

Sync 5일 때 0, Sync 180일 때 기본 확률의 2배가 되는 선형 보간 식입니다.

$$P\_{Sync} \= P\_{Base} \\times \\frac{(\\text{CurrentSync} \- 5)}{175} \\times 2$$

* **$P\_{Base}$:** Assault(5%), Scout(8%), Sniper(2%).

#### **단계 2: 상태 및 디버프 보정 ($P\_{Final}$)**

$$P\_{Final} \= P\_{Sync} \\times M\_{State} \\times M\_{Debuff}$$

* **$M\_{State}$ (상태 배율):**  
  * Hopeful(1.5), Inspired(1.2), Normal(1.0), Incapacitated(0.8), Fleeing(0.5), FriendlyFire(0.2).  
* **$M\_{Debuff}$ (디버프 배율):**  
  * $\\prod (1 \- \\text{Penalty})$  
  * 예: 화상 패널티가 0.1이면 $1 \- 0.1 \= 0.9$배를 곱함.

