# **게임 기획 문서 (GDD)**

## **1.0. 게임 개요 (Game Overview)**

* 장르 : 액티브 턴제 전술(전략적 판단, 피지컬 조작, 육성의 결합)  
* 컨셉 : 전략적인 배치와 수는 두되, 전투의 결과는 주사위에만 영향을 받는 것이 아닌 플레이어의 기술도 영향을 줄 수 있는 능동형 전술 게임.  
* **시점 :** **쿼터뷰 (Quarter View) 자유 시점.**  
  * *조작:* 마우스 우클릭 드래그를 통해 360도 자유롭게 회전 가능.  
* 시점 :  쿼터 뷰.(Free Rotation. 마우스 우클릭)  
* 플랫폼 : Unity 3D 엔진(URP환경)으로 제작. PC 출시 후 게임 패드 및 모바일 이식.  
* 타겟 :   어려운 난이도를 즐기지만 결과가 운에 의해 결정되는 것을 싫어하며, 수동적인 턴제의 진행 방식 대신 피지컬로 직접 개입하기를 원하는 유저.  
* **그래픽 컨셉 (Art) :** **다크 리얼리스틱 로우 폴리 (Dark Realistic Low Poly).**  
  * **형태는 단순하지만(Low Poly), 어둡고 무거운 조명과 안개 효과(URP)를 활용하여 디스토피아의 황량하고 압도적인 분위기를 연출함.**  
* **사운드 컨셉 (Audio) : 건조한 리얼리즘 (Dry Realism).**  
  * **웅장한 BGM은 배제하고, 앰비언트(바람 소리, 기계음) 위주로 구성.**  
  * **발소리, 호흡음, 총기 격발음, 피격음 등 전투 SFX를 극도로 강조하여 현장감 극대화.**


## **2.0. 핵심 게임 플레이 루프 (Core Gameplay Loop)**

### **2.1. 매크로 루프 (Macro Loop: 기지 관리 및 성장)**

플레이어는 엔딩을 볼 때까지, 전장에서 수집한 \*\*'잡동사니(Junk)와 부품'\*\*을 활용해 폐허가 된 기지를 재건하고 전력을 강화한다.

1\. 기지 관리 (Management Phase)

플레이어는 획득한 자원을 바탕으로 다음 시설들을 이용 및 강화한다.

* 기술소 (Tech Lab) \- \[핵심: Hideout System\]  
  * 역할: 기지 내 모든 시설의 기능을 확장하고 물리적으로 강화하는 건설/제작 허브.  
  * 방식: 단순 재화 소모가 아니라, \*\*전장에서 루팅한 특정 재료(전선, 금속판, 회로기판 등)\*\*를 직접 소모하여 시설 모듈을 건설함.  
  * 효과: 모듈 건설 시 해당 시설(의무실, 훈련소 등)의 기능이 영구적으로 향상됨.  
* 암시장 (Black Market) \- \[핵심: Exchange\]  
  * 역할: 자원 수급 불균형을 해소하는 물물교환 거래소.  
  * 기능: 남아도는 잡동사니를 팔아 자금을 확보하거나, 기지 확장에 필수적인 \*\*'희귀 부품'\*\*을 비싼 값에 구매/교환.  
* 병영 (Barracks)  
  * 대원 고용, 스쿼드 편성, 대원 상세 정보 확인.  
* 훈련소 (Training Grounds)  
  * 전투에 나가지 않는 대기 인원에게 경험치를 제공하거나 특성을 교정. (기술소 업그레이드에 따라 효율 상승)  
* 의무실 (Medical Bay)  
  * 부상당한 대원을 배치하여 체력 및 부상(트라우마) 회복. (기술소에서 침대 증설 시 수용 인원 증가)  
* 연구실 (Research Lab)  
  * 적의 '데이터(Data)'를 분석하여 기술적 해금 요소(신규 장비, 소모품 레시피)를 연구. (초반 잠금 $\\rightarrow$ 미션 진행 후 해금)  
* 기억의 방 (Memorial)  
  * 사망한 대원들의 기록(이름, 계급, 킬 수, 사망 원인)을 열람하고 추모.  
* 작전 통제실 (Operations Center)  
  * 월드맵을 통해 다음 수행할 미션(난이도, 보상, 적 유형)을 선택.  
  * **구현 로직**:  
    * **Global:** `MapCatalogManager`가 전역 `MapCatalogSO`를 참조하여 난이도별 풀에서 적절한 미션 후보(`MissionSO`) 리스트를 구성함.  
    * **Session:** 플레이어가 미션을 선택하면, \*\*Session Scope의 `MissionManager`\*\*가 해당 미션 정보(`MissionSO`)를 저장하여 씬이 변경되어도 데이터를 유지함.  
    * **Scene:** 전투 씬 진입 시 `SceneInitializer`가 `MissionManager`에 저장된 미션 정보를 참조하여 실제 맵 데이터를 로드하고 전투 환경을 구축함.  
* 수리공방 (Repair Shop)  
  *  **역할:** 전투에서 획득한 '손상된 장비'를 수리하여 사용 가능한 상태로 복구하는 시설.  
  * **기능:** 루팅된 무기는 기본적으로 '파손(Damaged)' 상태이며, 일정 자재(Junk)와 자금을 소모하여 수리해야 유닛에게 장착할 수 있습니다.

2\. 출격 준비 (Provision)

* 미션 시작 직전, 상점이 열리며 소모품(탄약, 수류탄, 회복약 등) 구매 및 **\[원정대 가방\]** 보관.  
* **시스템 초기화**: `BootManager`가 전역 및 씬 시스템을 부팅함. `UnitManager`는 `MapDataSO`를 순회하며 `RoleTag`가 설정된 타일을 찾아 해당 위치에 지정된 팀의 유닛을 생성 및 배치함.

3\. 전술 전투 (Tactical Combat)

* 적 제압뿐만 아니라, 맵 곳곳에 흩어진 \*\*'건축 자재(Loot)'\*\*를 확보하는 것이 중요한 전술적 목표가 됨.

4\. 결과 및 결산 (Result)

* **생존 대원 복귀 및 부상 처리:** 의무실 배치시 회복이 빨라짐  
* **사망 처리:** 사망 시 해당 대원이 장착했던 장비를 회수 하지 못했을 경우 영구 소실됨.  
* **전리품 확보:** 획득한 **재료 아이템(Junk)** 및 희귀 부품을 기지 창고에 저장.  
* 소모품 청산 (Supplies Liquidation):  
  * 원정대 가방에 남은 \*\*모든 소모품(탄약, 치료제, 수류탄 등)\*\*은 기지로 복귀하는 즉시 **자동으로 판매** 처리된다.  
  * **환불액:** 구매가의 \*\*30%\*\*에 해당하는 자금이 반환된다. (다키스트 던전 식 소모품 관리 방식 채택)

  ### **2.2. 마이크로 루프 (Micro Loop: 전투 턴 흐름)**

유니티의 TurnManager가 처리하게 될 1개 단위 턴의 로직 흐름.

* 1단계: 정보 수집 (Assessment): 플레이어는 360도 회전 카메라로 전장 상황(적 위치, 엄폐, 고저차) 파악.  
* 2단계: 행동 결정 및 계산 (Calculation): 이동 및 타겟 지정. 시스템은 사격 각도, 고저차, 실내/외 여부를 종합하여 '최종 명중률'과 '입력 난이도' 산출.  
* 3단계: 액션 실행 (Execution): 무기에 할당된 QTE 모듈(Timing, Hold, Focus) 팝업 및 입력 수행.  
* 4단계: 결과 적용 (Application): 최종 데미지 적용, 연출 재생, 행동 기회 소진 시 턴 종료.  
* 5단계: 적 반응 (Reaction): 적 턴에 아군 피격 시, \[위기 판정(치명타/사망)\] 조건 충족 시 방어 QTE 발동.

## **3.0. 세계관 및 시나리오 (World View & Scenario)**

### **3.1. 배경 (Background)**

* **디스토피아 SF:** 인류는 과도한 욕심과 자원 분쟁으로 인한 핵전쟁으로 이미 멸망함.  
* **AI의 목적:** 인류 멸망 후 남겨진 관리 AI는 '인간 복제 기술'을 연마하여 인간 세상을 재건하고, 그 위에 신처럼 군림하려 함.

### **3.2. 스토리텔링 구조 (Narrative Structure)**

표면적인 영웅 서사(Layer A)와 그 이면에 숨겨진 진실(Layer B)을 이중으로 전개한다.

**(1) Layer A: 표면적 설정 (Surface)**

* **설정:** 플레이어는 AI의 압제에 저항하는 '인간 레지스탕스'의 지휘관.  
* **전달 방식:** \*\*오퍼레이터(부관)\*\*의 비장한 브리핑.

**(2) Layer B: 실제 설정과 진실 (Hidden Truth)**

* **진실:** 플레이어 또한 AI가 보관 중인 **'통 속의 뇌(Brain in a Vat)'** 상태이며, 시뮬레이션 속에서 전술 데이터를 생성하기 위해 끝없는 전투를 반복하는 실험체.  
* **전달 방식:** 전장에서 수집하는 **\[로그 데이터 (Logs)\]**.  
* **로그 내용 (작성자 원문):**  
  * *"이상하다, 요즘 무언가 공허한 느낌이 들고 있다. 전쟁에 감정이 메말라 가는 것인가?"*  
  * *"얼마 전 어릴적부터 함께 해 온 강아지가 죽었다. 분명 슬프지만 눈물이 나지 않았다. 아니 슬프기는 한 것이 맞을까?"*  
  * *"옛날 SF 소설을 읽어보니 통속의 뇌와 같은 이야기를 읽었다. 핫 유치하기 짝이 없군. 때로는 생각한다 이런 지옥 속에서 사느니 통속의 뇌가 되는 것이 낫지 않을까? AI님 기왕이면 미녀 100명 정도 아내로 해서 실험해달라고\~ 그럼 당장 이 지긋지긋한 레지스탕스도 때려치지."*

### **3.3. 적의 정체 (Antagonist)**

* **설정:** AI가 보낸 치안 유지 로봇 및 변이된 생명체들.  
* **진실:** 이들은 인류를 멸망으로 몰고 갔다고 여겨지는 **'욕심', '질투', '분노'** 등의 감정이 데이터화되어 실체화된 존재들임.  
* **AI의 의도:** AI는 플레이어가 이들을 제압하는 과정을 통해 \*\*'불필요한 요소가 거세된 완전한 인간'\*\*의 알고리즘을 완성하려 함.

### **3.4. 결말 (Ending)**

* **진실의 대면:** 마침내 AI를 쓰러트린 주인공은 이 모든 것이 가상임을 깨닫고, AI는 플레이어를 통 속의 뇌에서 꺼내어 육체를 줌.  
* **최후의 선택:** 플레이어는 다음의 두 가지 층위에서 선택을 해야 함.  
  1. **AI에 대한 심판:** AI를 파괴할 것인가, 파괴하지 않을 것인가. (AI의 행동을 이성적으로 용인할 것인가, 부정할 것인가)  
  2. **자아의 선택:** 스스로를 파괴할 것인가, AI의 의도대로 살아남을 것인가. (AI의 요구에 순응하며 세뇌된 채 살아갈 것인가, 아니면 AI의 요구를 부수기 위해 자신의 목숨까지 포기할 것인가)  
     

## **4.0. 캐릭터 및 병과 시스템 (Character & Class System)**

*캐릭터의 성장은 \*\*'개인의 경험(Level)'\*\*과 **'기술의 발전(Research)'** 두 축으로 나뉜다.*

### **4.1. 기본 스탯 (Base Stats)**

*유닛은 다음의 핵심 능력치를 가지며, 일부는 내부 로직으로만 작동한다.*

* ***HP (체력):** 0이 되면 사망(영구적 죽음).*  
* ***Mobility (이동력):** 한 턴에 이동할 수 있는 타일 수.*  
* ***Aim (명중률):** 공격 시 액티브 미니게임의 **\[성공 영역(녹색/노랑)\]** 크기를 결정.*  
* ***Evasion (회피율):** 적의 명중률을 깎지 않는다. 대신 피격 시 발동하는 **\[방어 QTE\]의 성공 영역(Green Zone) 크기**를 결정한다. (회피율이 높을수록 방어 QTE가 쉬워짐).*  
* ***Agility:** 11.1절 공식에 따라 턴 순서를 결정하는 핵심 스탯.*  
* ***TS:** 0에 도달하면 턴을 획득하는 실시간 대기 수치.*

### **4.2. 병과 및 전용 무기 (Class & Weapon)**

*각 병과는 고유한 무기 기믹(QTE)과 4번(투척), 5번(특수기) 슬롯에 배정된 고유 기술을 가진다.*

**(1) 돌격병 (Assault)**

* **역할:** 중거리 딜러.  
* **무기 기믹: \[Hold & Release\]**  
  * **UI 디자인:** 수평 게이지 (Horizontal Bar) \- 왼쪽에서 오른쪽으로 차오르는 게이지.  
  * **조작 방식:** 버튼을 누르고 있으면 게이지가 **상승 하강 반복**, \*\*\[성공 영역\]\*\*에서 버튼을 뗌.  
* **고유 기술:**  
  * **\[Slot 4\] 파편 수류탄 (Frag):** (기본 범위 폭발).  
  * **\[Slot 5\] 제압 사격 (Suppression):** 특정 지역을 경계하며 해당 지역에서 움직임이 감지된 적에게 공격을 가함. QTE action은 따로 발동하지 않으며 계산된 최종 명중률에 따라 명중률 적용.

**(2) 저격병 (Sniper)**

* **역할:** 원거리 누킹.  
* **무기 기믹: \[Focus Aim\]**  
  * **UI 디자인:** 스코프 뷰 (Scope Circle) \- 화면 중앙 십자선과 흔들리는 조준원.  
  * **조작 방식:** 마우스/스틱으로 흔들리는 원을 중앙에 유지하며 시간 버티기.  
* **고유 기술:**  
  * **\[Slot 4\] 화염병/지뢰 (Area Denial):** 지역 장악용 투척 무기.  
  * **\[Slot 5\] 권총 사격 (Pistol Shot):** 중간 데미지, 짧은 사거리. **턴 강제 종료 없음**.

**(3) 스카웃 (Scout)**

* **역할:** 정찰(시야 확보).  
* **무기 기믹: \[Timing Hit\]**  
  * **UI 디자인:** **원형 링 (Radial Ring)**.  
  * **조작 방식:** 시계 방향으로 회전하는 바늘이 \*\*\[성공 영역\]\*\*을 지날 때 버튼 클릭 (리듬게임 스타일).  
* **고유 기술:**  
  * **\[Slot 4\] 스캔 수류탄 (Scan):** 시야 확보용.  
  * **\[Slot 5\] 은신 (Cloak):** 특정 반경 밖에서는 감지 불가 (공격 시 해제). 쿨타임 4턴.

(4) MVP

* **공통 메커니즘 (MVP):** 모든 주무기는 **\[Horizontal Hold & Release\]** 방식을 사용한다.  
  * **조작:** 버튼을 **누르고 있으면(Hold)** 게이지가 차오르고, 원하는 영역에서 **떼면(Release)** 발사된다.  
  * **왕복(Ping-Pong):** 게이지가 끝(100)에 도달하면 실패하지 않고 다시 0으로 줄어든다. (왕복 운동)

### **4.3. 대원 개인 성장 (Level Up)**

*전투에 참여하여 생존한 대원은 경험치를 얻고 계급이 상승한다.*

* ***보상:** 전투 능력치의 직접적인 강화. **(사망 시 영구 소실됨 → 상실감 유도)**.*  
  * ***스킬 해금:** 병과별 스킬 트리(공격형 vs 전술형)에서 스킬 선택 및 해금.*  
  * ***스탯 상승:** HP, 명중률(Aim), 치명타율 등 핵심 전투력 증가.*

### **4.4. 기술 연구 (Global Research)**

*기지에서 획득한 자원(데이터)을 소모하여 진행하는 전체 업그레이드.*

* ***보상:** 기반 시설 및 인프라 해금. **(대원의 죽음과 무관하게 유지됨)**.*  
  * ***장비 티어 해금:** 상점에서 상위 등급 장비 및 탄약 구매 권한 개방.*  
  * ***소모품 레시피:** 신규 수류탄, 회복 키트 레시피 해금.*  
  * ***기초 능력 보정:** 아군 전체 시야 거리 증가, 회복 속도 증가 등 보조적 효과.*  
  * 

## **5.0. 전술 환경 및 레벨 디자인 (Tactical Environment) \- *전면 수정***

전투는 **3D 공간의 그리드(Grid)** 위에서 진행되며, **시야, 고저차, 엄폐, 각도**가 상호작용하여 액티브 게이지의 난이도를 결정한다.

### **5.1. 시야 및 투사체 경로 (Vision & Path)**

| 항목 | 상세 규칙 및 기술 사양 |
| :---- | :---- |
| **360도 시야** | **모든 유닛은 전방향을 감시하며, 별도의 사각지대는 존재하지 않습니다.** |
| **차폐 및 투과** | • 벽(Wall) 및 천장(Ceiling): 사격을 완전히 차단합니다. • 창문(Window): 완전 투과(0.0) 방식으로 처리하며, 창문을 통과하는 사격에도 별도의 명중 패널티는 적용하지 않습니다. |
| **미식별 타겟 대응** | **시야가 확보되지 않은 안개 지역이라도 특정 타일(Grid Cell)을 지정하여 광역 공격을 시도할 수 있습니다. 공격에 적중하더라도 적의 위치는 드러나지 않고 은폐 상태를 유지합니다.** |
| **높이 규격 📏**  *(Height Standards)* | **• 유닛 신장:** 2.0 (히트박스 기준). **• 발사 원점 (Yshoot):** **1.8** (유닛의 눈/총구 높이). **• 낮은 엄폐물 (Hlow):** **1.2** (반엄폐). **• 높은 엄폐물 (Hhigh):** 2.5m (완전엄폐/벽). **• 로직:**  **Yshoot**(1.8) \>**Hlow**1.2이므로, 반엄폐 상태에서는 전방 사격 시 장애물 간섭이 발생하지 않음. |
| **레이캐스트 사양** | **3D 그리드 내 정확한 판정을 위해 시야 및 사격 레이캐스트는 반드시 지면이 아닌 발사 원점(1.8) 높이에서 시작하도록 고정합니다. (WYSIWYG 원칙 준수)** |
| **성능 최적화** | **시야 업데이트 트리거: ①유닛 이동 시(타일 단위), ②턴 시작 및 종료 시, ③폭발 등으로 인한 구조물 파괴(벽 제거) 이벤트 발생 시 즉시 해당 구역의 시야를 재계산함.** |
| **고지대 사각지대**  *(Dead Zone)* | **유닛이 벼랑 끝(Edge) 타일에 인접하지 않은 상태로 아래층을 볼 때, 일정 각도 이상의 사각지대에 있는 적은 감지할 수 없음. 즉, 아래층 적을 사격하려면 반드시 벽이나 낭떠러지 경계 타일로 이동해야 함.** |

### **5.2. 지형 속성 및 고저차 (Terrain Properties)**

타일은 높이(Level)와 **천장(Ceiling)** 속성을 가지며, 이는 투척 및 사격 경로에 영향을 준다.

0: 0층, 1: 1층... (2.5m 단위)

#### **(1) 층간 분리 (Layer Separation)**

* **이동 불가:** 든 수직 단차 이동 로직 삭제. **'순간이동(Teleport)'** 메커니즘으로 대체. 노드 간 직접 연결로만 이동.  
* **물리적 분리:** 모든 층은 물리적으로 완벽히 분리된 평면으로 취급된다.

#### **(2) 천장 유무 (Ceiling Property)**

* **이동과 무관:** 층간 이동이 '순간이동' 방식으로 변경됨에 따라, 천장은 더 이상 유닛의 물리적 이동을 막는 요소가 아니다.  
* **전술적 차단:** 대신 천장은 **'투사체(Projectile)'를 물리적으로 차단**하는 역할을 수행한다.  
  * **실내 (Indoor):** 천장이 있어 곡사 무기(수류탄 등) 사용이 불가능하며, 고지대에서의 사격 이점을 받을 수 없다.  
  * **실외 (Outdoor):** 천장이 없어 곡사 투척 및 고지대 낙차 사격이 가능하다.

#### **(3) 지형 파손 및 상태 변화 (Terrain State Transition):**

* **바닥 보존 법칙:** 수류탄 등 폭발 공격으로 인해 2층 바닥이 사라지는 '붕괴'는 발생하지 않으며, 모든 층의 바닥 타일은 물리적 위치를 유지함.  
* **파손 상태 (Broken State):** 특정 데미지 이상을 받은 바닥 타일은 `FloorType.BrokenConcrete` 등으로 상태가 변경됨.  
* **이동 제약:** 파손된 바닥은 시각적으로 파편이 표시되며, 해당 타일 통과 시 **이동 비용(Move Cost)이 증가**함 (예: 기본 1 → 파손 시 3).  
* **구현 로직:**  
  * `CombatManager`가 폭발 범위 내 타일의 `FloorID`를 변경.  
  * `MapManager`의 길찾기 알고리즘이 변경된 `FloorID`에 따른 가중치를 실시간 반영.

### **5.3. 엄폐 효율 결정 공식 (Combat Formula)**

엄폐물의 방어 효과는 고정값이 아니며, 공격자의 각도와 높이에 의해 실시간으로 **곱연산(Multiplicative)** 처리된다.

**FinalCover \= BaseCoverValue × AngleFactor × HeightFactor** \> 1\. **BaseCoverValue:** 반엄폐(20), 완전엄폐(40).

2\. **AngleFactor:** max(0,Vcover ᐧ VAttack). (벡터 내적값 사용)

3\. **HeightFactor:** max(0.8,1.0 \- (ΔH ×0.05) (층수 차이당 5% 감소, 최대 20%)

#### **(1) 각도 계수 (Angle Factor) \- *벡터 내적(Dot Product) 사용* 인위적인 구간 나누기 없이, \*\*벡터 내적값(Cosine)\*\*을 그대로 사용하여 각도가 벌어질수록 엄폐 효율이 자연스럽게 감소한다.**

* **공식:**  
  **AngleFactor \= max(0, V\_Cover · V\_Attack)**

	**V\_Cover : 엄폐물이 바라보는 정면 벡터 (Normalized)**

	**V\_Attack : 타겟에서 공격자를 바라보는 방향 벡터 (Normalized)**

* **적용 예시:**  
  * **정면 (0°): 계수 1.0 (효율 100%)**  
  * **45°: 계수 0.7 (효율 70%)**  
  * **60°: 계수 0.5 (효율 50%)**  
  * **측면/후방 (90° 이상): 계수 0.0 (엄폐 무시)**

#### **(2) 높이 계수 (Height Factor)**

공격자가 타겟보다 높은 곳에 위치할 경우 엄폐 효율을 깎는다.

* **기본 로직:** 층수 차이당 5% 감소" 로직 유지. 단, "실제 높이(2.5m \* 층수)가 아닌 논리적 층수(ΔH)를 기준으로 계산함  
* **예외 (Indoor Exception):** 타겟이 \*\*\[실내\]\*\*에 있고 **\[완전 엄폐\]** 중이라면, 천장에 의해 사격각이 제한되므로 **높이 계수는 1.0(페널티 없음)으로 고정**된다.

#### **(3) 최종 명중률 산출**

* HitChance \= AttackerAccuracy \- FinalCover  
* 엄폐율은 0 미만으로 떨어지지 않으므로(Clamp), 명중률이 비정상적으로 상승하는 현상은 방지된다.


#### **(5) 낙하 데미지 공식 (Fall Damage) :**

* **원칙:** 고정값(Absolute Value) 기반 데미지를 적용한다.  
* **공식:** Dfall= max(0, (ΔH-1) \* 10  
* **설명:** 1층 높이(ΔH=1) 추락 시 데미지 0, 2층 높이 추락 시 데미지 10, 3층 높이 추락 시 데미지 20을 부여한다.

### **5.4. 액티브 게이지 영역 산출 규칙 (The 70/30 Rule)**

**(1) 영역 배분 로직 (Allocation Logic)**

* **전체 길이:** 100 (고정).  
* **성공 영역 (Success Zone):** 최소 5% \~ 최대 70%. (명중률에 비례).  
* **내부 비율:** 성공 영역 내에서 \*\*정타(Yellow) : 스침(Green)\*\*은 **7 : 3** 비율 기본.  
* **치명타(Red):** 정타(Yellow) 영역을 잠식하며 생성.

**(2) 배치 규칙 (Positioning)**

* **랜덤 배치 (Random):** 성공 영역의 위치는 고정되지 않으며, 매 QTE 시작 시 **무작위 위치**에 생성된다. (단, 게이지 양 끝 5% 여유 공간 제외).

  ### **5.5. 특수 지형지물 (Special Objects)**

* **창문 (Window):**  
  * **시야:** 투명하게 처리되어 시야를 차단하지 않음.  
  * **엄폐:** 부분 엄폐 효과 제공.  
  * **투척:** 투사체가 통과 가능함.  
* **문 (Auto-Door):**  
  * 항상 열린 상태.   
* **구조적 파괴 및 이동성 복구 (Destruction & Recovery):**  
  * **파괴 대상: 맵의 기둥(Pillar)과 벽(Wall)은 고유 HP를 가지며, 수류탄 등의 공격으로 파괴될 수 있음.**  
  * **점유 해제 (De-occupation): 타일 중심을 차지하던 기둥이 파괴되면, 해당 타일의 점유자 목록에서 즉시 제거됨.**  
  * **이동성 복구: 기둥이 사라진 타일은 즉시 `UpdateCache()`를 실행하여 `IsWalkable` 상태를 `True`로 전환함.**  
  * **구현 로직:**  
    * **`CombatManager`가 `PillarInfo.TakeDamage()` 호출.**  
    * **기둥 HP 0 도달 시 `Tile.RemoveOccupant(PillarInfo)` 실행.**  
    * **`Tile.UpdateCache()`에 의해 `_cachedIsWalkable`이 `True`로 갱신되며 `OnWalkableStatusChanged` 이벤트 발생.**  
    * **`TilemapGenerator`가 기둥 모델을 삭제하고 아래에 깔려 있던 바닥 모델을 노출함.**  
  * **파괴 데이터 갱신 흐름:**  
1. **파괴 시 `Tile.OnEdgeDestroyed` 이벤트 발생.**  
2. **`TilemapGenerator`가 이벤트를 수신하여 벽/기둥 오브젝트 삭제 및 파편 생성.**  
3. **`EnvironmentManager`가 파괴를 감지하고 `Tile.UpdateCache`를 호출하여 실시간 이동 가능 여부(IsWalkable) 재계산.**  
* 타일 점유(Tile Occupation):  
  * 유닛, 파괴된 차량 등 일부 오브젝트는 타일 전체를 차지\*하며, 이 타일은 다른 유닛이 통과하거나 멈출 수 없다  
  * 길찾기 시 '이동 불가' 지역으로 취급된다.  
  * 오브젝트의 종류에 따라 '타일 점유' 상태이면서도 이동 비용(Cost)만 높이는 경우도 존재할 수 있다 (예: 낮은 상자).  
* 경계 엄폐(Edge Cover):  
  * 낮은 벽, 난간 등의 엄폐물은 타일과 타일 사이의 '경계'에 위치\*하는 것으로 취급된다.  
  * 이는 이동을 막지 않지만(반엄폐의 경우), 사격 각도에 따라 엄폐 보너스를 제공하는 역할을 한다.  
* **연결 오브젝트 (Connector Object)**  
  * **개요:** 물리적으로 떨어진 두 좌표(Node A ↔ Node B)를 논리적으로 연결하여 이동 경로를 제공하는 오브젝트.  
  * **이동 방식 (Interaction):** 유닛이 진입점(Start Node)에서 상호작용 시, 연결된 도착점(End Node)으로 \*\*즉시 이동(Teleport)\*\*한다..

### **5.6. 맵 데이터 구조 (Map Data Structure)**

맵의 최소 단위인 '그리드 셀(Grid Cell)'은 단순한 데이터 덩어리가 아니라, \*\*바닥(Center), 벽(Edge), 기둥(Pillar)\*\*의 역할이 명확히 구분된 조립 데이터이다. 대규모 맵(130x130)의 모바일 구동을 위해 **메모리 최적화 구조**를 따른다.

#### **1\. 기본 좌표 및 규격 (Coordinates & Specs)**

* **좌표계:** (Col, Row, Level)의 3차원 정수 좌표. (GridCoords 구조체 사용)  
* **좌표계 원점:** 논리적 (0, 0, 0)은 월드 좌표 (0, 0, 0)에 매핑되는 절대 좌표계.  
* **그리드 피벗:** 타일의 중앙(Center)을 기준으로 함.  
* **월드 좌표 변환:** 그리드 (x, z) 타일의 월드 좌표 X, Z는 x × CELL\_SIZE 및 z × CELL\_SIZE이다.

#### **2\. 기술적 구현 사양 (Technical Implementation) \- *최적화 적용***

* **런타임 (In-Game):**  
  * **Pure Class: 메모리 최적화를 위해 Tile은 `MonoBehaviour`가 아닌 순수 C\# 클래스로 정의한다.**  
  * **희소 배열 (Sparse Array): 데이터가 없는 좌표는 `null`로 유지하여 모바일 메모리 한계를 극복한다.**  
* **에디터 (Map Editor):**  
  * **Scene-First: 편집 중에는 `EditorTile` (MonoBehaviour) 프리팹을 씬에 직접 배치하여 Unity의 기본 툴(이동, 회전, Undo)을 활용한다.**  
  * Baking: 저장(Save) 시점에만 씬의 `EditorTile` 객체들을 순회하여 `MapDataSO`의 순수 데이터로 직렬화(Serialize)한다.  
  * 계층 구조 생성 (Layered Generation): 모든 객체화는 \[타일(Floor) → 구조물(Edge/Pillar)\] 순서로 수행함. 타일 객체를 먼저 생성한 뒤, 그 위에 벽 정보를 설정하거나 기둥(PillarInfo)을 점유자로 등록함.

#### **3\. 구성 요소 (Cell Composition)**

(1) 바닥 셀 (Floor Cell)

유닛이 이동하고 전투를 벌이는 기본 타일이다. 성능 향상을 위해 점유 슬롯을 명시적으로 분리한다.

* **센터 (Center):**  
  * **FloorID (Enum):** 바닥재 재질. **\[예외 처리\]** 이 값이 None이거나 Null일 경우, 해당 좌표는 물리적으로 뚫려있는 구멍으로 간주하며, 유닛 진입 시 추락(Fall) 판정이 발생한다.  
  * **점유 슬롯 (Occupancy Slots) \- *변수 분리*:**  
    * **PrimaryUnit (Unit):** 타일을 점유 중인 유닛 혹은 장애물 객체  
    * **RoleTag (string):** 타일 자체에 부여된 역할 식별자 (예: "PlayerSpawn", "EnemySpawn\_Elite"). 별도의 스폰 포인트 오브젝트 없이 타일 데이터만으로 배치 로직을 수행하는 SSOT(Single Source of Truth) 방식임.  
    * **LootItems (List\<Item\>):** 바닥에 떨어진 아이템 목록.  
  * **캐싱 (Caching):** 매번 리스트를 검사하지 않고, PrimaryUnit의 상태에 따라 IsWalkable 프로퍼티를 자동 갱신하여 길찾기 연산 속도를 보장한다.  
* **엣지 (Edge \- North/East/South/West):**  
  * 타일의 사방 경계에 종속되는 데이터.  
  * **유형:** Wall(통벽), Window(창문), Door(문), None(개방).  
  * **데이터 최적화:** 런타임에는 가벼운 `EdgeInfo` 구조체를 사용하여 물리 연산을 처리한다.  
  * 데이터 저장 (Persistence):  
    * 파일 저장 시에는 **`SavedEdgeInfo`** 구조체를 사용하여 데이터 손실을 방지한다.  
    * **저장 항목:** `EdgeType` (벽/창문), `CoverType`, `MaxHP`, **`CurrentHP` (파괴 상태 보존)**  
    * **목적:** 로드 시 파괴된 벽이 복구되거나, 콘크리트 벽이 투명 벽으로 변하는 데이터 무결성 오류를 원천 차단함.  
  * **데이터 무결성 (Data Integrity \- Sync Rule):**  
    * 엣지 데이터는 인접한 두 타일이 공유하는 물리적 벽면이므로, **반드시 양방향 동기화**되어야 한다.  
    * **규칙:** **참조형 클래스(RuntimeEdge) 활용:**   
      * 런타임에서는 엣지 정보를 구조체가 아닌 `RuntimeEdge` \*\*클래스(Class)\*로 관리함.  
      * **공유 메커니즘:** `EnvironmentManager`가 초기화 시 인접한 두 타일에 \*\*동일한 `RuntimeEdge` 객체 참조를 주입(Wiring)\*\*함. 이를 통해 한쪽 타일에서 벽을 파괴(데미지 적용)하면 연결된 반대편 타일의 엣지 상태도 별도의 복사 과정 없이 실시간으로 동기화됨.

(2) 기둥 셀 및 PillarInfo (Pillar Cell & Logic)

* **PillarID (Enum):** 기둥의 외형 및 내구도 정보 식별.  
* **PillarInfo 객체:** 단순 데이터가 아닌 `ITileOccupant` 인터페이스를 구현한 **논리 점유 객체**임.  
* **점유 시스템 연동:** 타일 생성 시 `AddOccupant(PillarInfo)`를 통해 등록되며, 기둥의 HP가 0이 되어 파괴될 경우 `OnBlockingChanged` 이벤트를 발생시켜 타일의 `IsWalkable` 상태를 즉시 `True`로 전환함.  
* **논리적 구축:** `MapManager` 초기화 시 기둥 데이터를 읽어 `PillarInfo`를 생성하고, 타일의 `AddOccupant()`로 등록함. 등록 시 타일의 `UpdateCache()`가 실행되어 해당 타일의 `_cachedIsWalkable`을 `false`로 자동 확정함.  
* **특성:**  
  * 엣지(Edge) 데이터를 가지지 않으며, 좌표 전체를 물리적으로 점유함.  
  * 유닛 진입 불가(Impassable).  
  * 지지력($S=5$)의 원천이 되어 상층 타일을 지탱함.

    #### **4\. 규격 및 매핑 (Specs & Mapping)**

* **축 매핑 (Axis Mapping):**  
  * **North (Index 0):** \+Z 방향  
  * **East (Index 1):** \+X 방향  
  * **South (Index 2):** \-Z 방향  
  * **West (Index 3):** \-X 방향  
* **수직 규격 (Vertical Spec):**  
  * **층고 (LEVEL\_HEIGHT):** **2.5m**  
  * **임계값 (Threshold):** 50% 반올림 방식을 사용하여, 현재 높이가 **±1.25m** 범위를 넘을 시 층간 이동으로 간주함.  
  * **바닥 오프셋 (FLOOR\_OFFSET):** 0.2m (바닥 오브젝트의 두께를 고려하여 유닛의 실제 서 있는 높이를 보정함).

### **5.7. 모듈형 건축 시스템 (Modular Construction System)**

레벨 디자인은 개별 타일을 배치하는 것이 아니라, 구조화된 모듈을 조립하는 방식을 따른다.

* **조립 규칙 (Assembly Rule):**  
  * **벽(Wall):** 타일의 좌표를 점유하지 않고, 타일과 타일 사이의 '경계선'에 부착된다.  
  * **창문(Window):** 벽과 동일하게 경계선에 부착되나, `IsTraversable` 속성을 가져 이동 및 사격이 가능하다. (넘어가기 연출 필요)  
  * **적층(Stacking):** 바닥 셀과 기둥 셀을 각각 독립된 좌표에 쌓는 방식을 취한다. 기둥 셀은 상하로 인접한 바닥 셀들에게 지지력을 전파하여 다층 구조를 유지시킨다.


### **5.8 이동 경로 시각화**

* **범위:** 이동 가능 최대 반경 → **녹색(Green) 오버레이**.  
* **경로(Valid):** 마우스 오버 시 이동 경로 → **파란색(Blue) 라인/타일**.  
* **경로(Invalid):** 이동력 초과 또는 이동 불가 지역(오브젝트/유닛 점유) → **빨간색(Red) 라인/타일**.  
* **복합 경로:** 갈 수 있는 데까지는 파란색, 그 이후부터 빨간색으로 이어지는 **'부분 경로 표시'** 로직 명시.

### **5.9 미션 및 스폰 시스템 (Mission & Spawn)** 

* ###  **MissionSO 구조: 기존 `MapEntry`를 대체하는 데이터 컨테이너.**

  * `MapDataRef` (AssetReference): 전투용 맵 데이터.  
  * `MaxPlayerUnits` (int): 출격 가능 아군 수.  
  * `EnemySpawns` (List): `EnemyData`, `SpawnRoleTag`, `Count` 정보를 포함하는 리스트.  
  * `Rewards`: 클리어 시 획득할 `ItemDataSO` 및 자원 보상.

* ###  **분대(Pod) 스폰 로직:**

  * `SpawnRoleTag`와 일치하는 타일을 앵커(Anchor)로 설정.  
  * 

# **6.0. 기술 아키텍처 (Technical Architecture)** 

본 프로젝트는 **Unity 3D (URP)** 환경을 기반으로 하며, **Service Locator 패턴**과 **Initializer 패턴**을 결합하여 싱글톤의 폐해(결합도, 순서 문제)를 해결하고, 메모리 생명주기(Lifecycle)를 엄격히 통제한다.

#### **6.0.1. 아키텍처 핵심 원칙 (Core Principles)**

1. **서비스 로케이터 (Service Locator): 모든 매니저는 `ServiceLocator`를 통해서만 접근한다.**  
2. **하이브리드 등록 (Hybrid Registration): Global 매니저는 자신의 Awake()에서 스스로를 등록한다(자가 등록). 반면, 씬의 생명주기에 종속적인 Session 및 Scene 매니저는 SceneInitializer에 의해 중앙에서 등록을 관리한다(중앙 등록). 이를 통해 각 스코프의 특성에 맞는 등록 방식을 사용한다.**  
3. **3계층 생명주기 (3-Tier Lifecycle):**  
   * **Global: 앱 시동 시 등록, 영구 유지 (`AppBootstrapper`).**  
   * **Session: 게임 시작 시 등록, 타이틀 복귀 시 파괴 (`GameSession` \- 미션 데이터 유지).**  
   * **Scene: 씬 로드 시 등록, 씬 종료 시 파괴 (`SceneInitializer`).**  
4. **2단계 초기화 (Two-Phase Initialization):**  
   * **1단계 (등록 \- Awake): 모든 매니저는 `Awake()`에서 `ServiceLocator`에 자신을 등록하며, 타 시스템 참조는 금지함.**  
   * **2단계 (컨텍스트 주입 \- Initialize): `BootManager`에 의해 트리거된 Initializer가 `InitializationContext`를 생성하여 각 매니저의 `Initialize(context)`를 호출함**  
   * **데이터 일관성: `InitializationContext`는 실행 시점에 필요한 `GlobalSettings`, `TileRegistry`, `MapData` 등을 한꺼번에 담아 전달함으로써 매니저 간 데이터 불일치를 방지함**  
5. **인터페이스 기반 비동기 초기화:** 초기화가 필요한 모든 매니저는 `IInitializable`을 구현하며, 반환 타입은 반드시 \*\*`UniTask`\*\*여야 한다. 메인 스레드 블로킹(`WaitForCompletion`)은 어떠한 경우에도 금지한다.  
6. **결함 감지 및 차단 (Fail-Fast):** 초기화 도중 치명적인 예외(Exception) 발생 시, 즉시 부팅 절차를 중단하고 에러 로그를 출력한 뒤 애플리케이션을 종료하거나 에러 상태(`SessionState.Error`)로 전이한다.  
7. 대규모 맵 로딩 전략 (Loading Strategy):  
   * **시간 할당제 스트리밍 (Time-Sliced Streaming):** 130x130 이상의 대규모 맵 로딩 시, 타일 생성 루프가 메인 스레드를 독점하여 화면이 멈추는(Freezing) 현상을 방지한다. 타일 생성 시 '개수' 기준이 아닌 \*\*"프레임당 16ms(0.016초) 제한"\*\*을 두어, 시간이 초과되면 작업을 일시 중단(`await UniTask.Yield`)하고 제어권을 OS에 넘긴다. 이를 통해 로딩 중에도 UI 애니메이션과 터치 반응성을 유지한다.  
   * **온디맨드 생성 (On-Demand Generation):** 초기화 시 `new Tile()`을 맵 전체에 수행하지 않는다. `MapData`에 유효한 정보가 존재하는 좌표에 대해서만 객체를 생성하여 런타임 메모리 스파이크를 방지한다.   
8. **데이터 주입 (Injection):** Global → Session → Scene 순으로 컨텍스트를 전달하여 데이터 일관성을 보장한다.

---

## **6.1. 개발 환경 및 기반 기술 (Development Stack)**

| 구분 | 기술 스택 / 버전 | 선정 이유 및 비고 |
| :---- | :---- | :---- |
| **엔진** | **Unity 2022.3.62f1 (LTS)** | 장기 지원 버전(LTS)을 사용하여 개발 도중 엔진 버그로 인한 리스크 최소화. |
| **렌더링** | **URP** (Universal Render Pipeline) | 로우 폴리 그래픽에 최적화된 성능. Shader Graph를 통한 캐릭터 실루엣(X-Ray), 포스트 프로세싱(Bloom) 구현 용이. |
| **입력** | **New Input System** | 키보드/마우스 및 게임패드 동시 지원. Action Map(Menu, Gameplay, QTE) 분리 용이. |
| **비동기** | **UniTask** | 코루틴(Coroutine) 대비 가독성이 높고 오버헤드가 적은 async/await 패턴 사용. "Unity의 Native Coroutine 및 Addressables의 동기 함수(`WaitForCompletion`) 사용을 금지하고, 모든 비동기 로직을 UniTask로 통일." |
| **리소스** | **Addressables \+ Direct Ref (Hybrid)** | **\[하이브리드 리소스 정책\]**  • **Heavy Assets (VFX, Prefab, Audio):** `AssetReference`를 사용하여 비동기 로드(메모리 절약). • **Light Assets (UI Icon, SO Data):** `Sprite/Type` 직접 참조를 사용하여 즉각적인 UI 갱신(UX 향상) 및 구현 복잡도 감소.  |
| **데이터** | **ScriptableObject (SO)** | 기획 데이터(밸런스, 맵 정보)와 로직의 분리. JSON 직렬화 연동. |

---

## **6.2. 시스템 계층 구조 (System Hierarchy) \[수정됨\]**

매니저의 생명주기와 등록 주체에 따라 \*\*Global(App)\*\*, Session 과 **Scene(Session)** 두 가지 스코프로 명확히 구분한다. 

**Global/Session/Scene Scope 공통:** "모든 매니저는 독립적인 모듈로서 스스로를 로케이터에 등록하며, Initializer는 오직 생성과 비동기 초기화 트리거(Trigger) 역할만 수행한다."

### **(1) Global Scope (App Lifetime)**

* **생명주기:** 앱 실행(Boot) \~ 종료(Quit). `DontDestroyOnLoad`.  
* **등록 방식:** **자가 등록 (Self-Registration)**. Awake()에서 ServiceLocator.Register(this) 호출.  
* "단, `AppInitializer`는 매니저를 **생성(Instantiate)만** 하며, 등록 코드를 포함하지 않는다(의존성 제거)."  
* **초기화 주체:** AppInitializer (Addressables 로드 및 생성 담당).

| 매니저 이름 | 핵심 역할 | 주요 책임 및 기능 |
| :---- | :---- | :---- |
| **ServiceLocator** | **연결** | 모든 매니저의 등록/해제 및 접근점 제공 (싱글톤 대체). |
| **AppBootstrapper** | **시동** | "앱의 진입점(Entry Point). `AppConfig` 로드 및 Global 매니저 생성(Instantiate)을 담당한다. **\[개발 편의성\]:** `BootScene`이 아닌 일반 씬에서 실행 시, 자동으로 Global 환경을 구축(Bootstrap)하여 개발 속도를 저하시키지 않도록 한다." 단, 자동 부트스트랩의 호출(Trigger)은 SceneInitializer가 수행한다 |
| **GameManager** | **총괄** | 게임의 최상위 상태(Main Menu ↔ InGame) 관리. |
| **DataManager** | **DB** | 게임 내 모든 정적 데이터(SO) 및 세이브 데이터 로드/캐싱. |
| **InputManager** | **입력** | 하드웨어 입력을 게임 Action으로 변환 및 레이어(UI/Game) 제어. |
| **GlobalSettingsSO** | **설정** | 해상도, 사운드 볼륨 등 전역 설정 데이터 컨테이너. |
| **SaveManager** | **저장** | 로컬 파일 기반 세이브 데이터 입출력 관리. |

### **(2) Session Scope (Session Lifetime)**

* **생명주기:** 전투 씬(InGame) 로드 시 생성 \~ 씬 언로드 시 파괴.

| 매니저 이름 | 핵심 역할 | 주요 책임 |
| :---- | :---- | :---- |
| **MissionManager** | **세션 미션** | **선택된 MissionSO 데이터 유지 및 씬 간 중계.** |
| **PlayerRosterManager** | **부대 관리** | **(예정) 아군 유닛들의 상태 및 성장 데이터 보존.** |
| **InventoryManager** | **아이템 관리** | **(예정) 세션 내 획득 아이템 및 인벤토리 관리.** |
| **MapCatalogManager** | **미션 DB** | `MapCatalogSO`를 인덱싱하여 난이도/태그별 맵 검색 API를 제공하는 DB 역할 |
| **BaseManager** | **기지 관리** | • 기지 내 UI 메뉴(연구, 병영, 정비) 네비게이션 처리. • SquadManager와 연동하여 대원 치료/훈련 로직 수행. |

### **(3) Scene Scope (Level Lifetime)**

| 계층 (Layer) | 매니저 이름 | 주요 책임 |
| :---- | :---- | :---- |
| **Initializer** | **SceneInitializer** | •"씬 내 매니저의 생성(RegisterOrSpawn) 후, `Start()` 단계에서 \*\*비동기 루프(Async Loop)\*\*를 통해 순차적으로 `Initialize()`를 호출한다. 하나라도 실패 시 게임 진입을 차단한다." • 게임 종료(OnDestroy) 시 등록의 역순으로 안전하게 해제. |
| **State Machine** | **SessionManager** | • 전투의 흐름(FSM) 관리 (Boot → Setup → TurnWaiting → UnitTurn...). • IInitializable 구현 및 상태 전이 제어. |
| **System** | **TurnManager** | • 유닛의 TS(Turn Speed) 계산 및 턴 큐(Queue) 관리. |
|  | **MapManager** | • 맵 생성(WFC/Module), NavMesh 베이크, 엄폐물 데이터 관리. |
| **Combat** | **CombatManager** | • 공격/피격 판정 공식 계산 및 데미지 적용. |
|  | **UnitManager** | • 유닛 스폰, 사망 처리, 리스트 관리. |
| **Visual / UI** | **CameraManager** | • 시점 제어 및 액션 캠 연출. |
| **Visual** | **TilemapGenerator** | MapManager의 논리 데이터를 기반으로 실제 프리팹을 월드에 비동기(GenerateAsync) 스폰 및 배치. |
|  | **DamageTextMgr** | • 데미지 플로팅 텍스트 연출. |
| **System** | **EnvironmentManager** | • **환경 변화 중계:** 벽/기둥 파괴 이벤트를 수신하여 데이터와 비주얼 간의 동기화 명령 하달. 333  • **논리 갱신 트리거:** 구조물 제거 시 Tile.UpdateCache()를 호출하여 이동 가능 여부 및 시야를 실시간 재계산하게 함.   • **상태 변환 관리:** 바닥 타일의 파손 상태(FloorType 변경)와 그에 따른 이동 비용(Cost) 가중치 적용 관리.  |
|  | **TileDataManager** | `TileRegistry`를 보유하며, 런타임에 필요한 타일/벽/기둥의 프리팹과 속성 데이터를 공급함. |
|  | **PathVisualizer** | `이동 범위 및 경로 시각화.` |
|  | **QTEManager** | `무기별 QTE 모듈 실행 및 결과 판정.` |
|  | **DamageTextManager** | `데미지 플로팅 텍스트 팝업 관리.` |
|  | **TargetUIManager** | `타겟 유닛 정보 표시 관리.` |
|  | **PlayerInputCoordinator** | `입력을 활성 컨트롤러로 전달.` |

### **6.3. 핵심 시스템 구현 로직 (Core Implementation Logic)**

#### **1\. 물리 레이어 구성 (Physics Layers)**

Raycast 판정의 정확도를 위해 레이어를 명확히 구분한다.

| 레이어 이름 | 용도 | Raycast 활용 예시 |
| :---- | :---- | :---- |
| **Ground** | 바닥/지형 | 마우스 클릭 이동 지점 판별. |
| **Wall** | 높은 벽 | 높은 엄폐 판정, 이동 차단. |
| **Cover** | 낮은 엄폐물 | 엄폐 판정, 이동 차단. |
| **Unit** | 캐릭터 | 공격 타겟팅 판별. |
| **Ceiling** | 지붕/천장 | 실내 판정, 곡사 화기(투척) 차단. |

#### 

#### **2\. 전투 공식 파이프라인 (Combat Formula Pipeline)**

공격 시도 시 다음 순서로 연산하여 최종 결과를 도출한다.

1. **각도 계수 (FAngle) 산출 :**  
   * 벡터 내적: D \= **VCover ᐧ VAttack**  
   * **FAngle \= Mathf.Max(0,D)**

**2\. 높이 계수(FHeight) 산출 :**

* 층수차이 : ΔH \= Attacker.Layer \- Target.Layer  
* 실내 예외 처리 : 타겟이 Indoor이고 엄폐물이 High Cover면 ΔH \= 0 으로 강제.  
*  FHeight \= max(0.80,1.0 \- (ΔH x 0.05))

**3\. 최종 엄폐율(RCover) 산출 :** 

* **RCover \= BaseCover X FAngle X FHeight**

**4\. 최종 명중률(Phit) 산출 :** 

* **PHit \= Attacker.Accuracy \- RCover**

#### **3\. 시체 루팅 및 파밍 시스템 (Looting Logic)**

* **드랍 생성:** 적 사망(`Unit.Die()`) 시 `LootTableSO`를 참조하여 확률적으로 아이템을 생성한다.  
* **획득 방식 (Interaction):**  
  1. 플레이어 유닛이 전리품(시체/상자) 인접 타일로 이동.  
  2. **\[조사하기\]** 액션 실행 (행동력 소모 없음).  
  3. 획득한 아이템은 즉시 \*\*\[원정대 가방\]\*\*으로 들어간다.  
* **전투 종료 정산 :**  
  1. 전투가 끝나면 원정대 가방의 내용물을 분류한다.  
  2. **소모품(탄약/치료제):** 자동 판매(환불) 처리.  
  3. **재료(Junk/Parts):** 기지 창고(Inventory)로 영구 보관.

#### **4\. 이동 및 길찾기 (Movement & Pathfinding)**

* **1\) 알고리즘 및 휴리스틱 (Algorithm & Heuristic)**  
  * **기술: 경로 탐색은 *A 알고리즘*\*을 사용하며, H(n) 남은 거리 추정) 계산 시 맨해튼 거리(Manhattan Distance) 공식을 사용한다.**  
  * D \= | x1 \- x2 | \+ | z1 \- z2 |  
* 2\) 이동 비용 산출 (Cost Formula)  
  * **기본 원칙:** 길찾기는 최단 거리(Shortest Distance)가 아닌 **'최소 비용(Minimum Cost)'** 경로를 탐색한다.  
  * **수평 이동 (Move):** 동일 층 내 인접 타일(동/서/남/북)로 이동 시 \*\*기본 비용 1 (Base Cost 1)\*\*을 소모한다.  
  * **수직 이동 (Interaction):** 층간 이동은 이동 비용 공식에 포함되지 않으며, 별도의 \*\*'상호작용(Interaction)'\*\*으로 처리한다. (거리/높이 무관 **고정 비용 1**).  
* **3\) 단층 길찾기 원칙 (Single-Layer Navigation)**  
  *  **범위: 시스템의 자동 경로 탐색은 현재 유닛이 위치한 층(Current Y-Level) 내에서만 수행한다. 다른 층으로의 경로는 자동 계산하지 않는다.**  
  * **플레이어의 역할: 층간 이동이 필요한 경우, 플레이어가 직접 유닛을 순간이동기(Teleporter) 타일로 이동시킨 후 상호작용해야 한다**  
* 4\) 이동 제약 조건 (Constraints)  
  * 길찾기 및 이동 시 다음 조건을 모두 만족해야 한다.

  1\. 이웃 타일이 맵 범위 안에 있는가?

  2\. 이웃 타일이 다른 유닛이나 장애물(Pillar 등)에 의해 \*\*'점유(Occupied)'\*\*되어

      있지 않은가?

  3\. 현재 타일과 이웃 타일 사이의 '경계'가 \*\*완전 엄폐(Full Wall)\*\*로 막혀있지

    않은가?

* **시각적 피드백:** 층간 이동이 가능한 타일(순간이동기)은 전술 뷰에서 식별 가능한 색상(예: 노란색)으로 하이라이트한다.

#### 

#### **5\. 탐험 및 자동 스폰 흐름 (Exploration & Auto-Spawn)**

1. **탐험 단계:** `MapCatalogManager`에서 추천된 미션 중 하나를 선택, `MissionManager`(Global/Lazy)에 저장.  
2. **전투 로드:** `SceneInitializer`가 `MissionManager`를 확인하여 맵 데이터를 비동기 로드.  
3. **분대 스폰:** `UnitManager`가 `MissionSO`에 정의된 `EnemySpawns` 리스트를 순회하며 앵커 타일 기준 **Scattered Spawning** 수행.

#### **6\. 유닛-컨트롤러 아키텍처 (Unit-Controller Architecture)**

본 프로젝트는 유닛의 데이터/상태와 제어 로직을 명확히 분리하는 컨트롤러 패턴을 따른다.

* \`Unit\` (\`UnitStatus.cs\`의 역할):  
  * 전장에 존재하는 모든 개체(아군, 적군)를 의미한다.  
  * 자신의 모든 상태(HP, TS, 위치, 보유 어빌리티 등)와 기본 데이터(UnitDataSO)를 소유한다.  
  * 외부의 '명령'에 따라 자신의 상태를 변경하거나 행동(이동, 피격 등)을 실행할 뿐, 스스로 행동을 '결정'하지  않는다  
* \`Controller\` (\`PlayerController.cs\`, \`AIController.cs\`의 역할):  
  * Unit을 소유(Possess)하여 조종하는 '두뇌'에 해당한다.  
  * TurnManager로부터 자신의 유닛이 턴을 획득했음을 통지받으면 활성화된다.  
  * \`PlayerController\`는 사용자의 입력을 받아 Unit에게 명령을 내린다.  
  * \`AIController\`는 자체적인 AI 로직에 따라 판단하여 Unit에게 명령을 내린다.


#### **7\. 사망 장비 회수 및 임시 창고 로직** 

**사망 발생:** 아군 유닛 HP가 0이 되어 사망(`Dead`) 상태 전환.  
**자동 탈착:** 해당 유닛의 `MainWeapon`과 `BodyArmor`가 즉시 장착 해제됨.  
**가방 전송:** `InventoryManager`(공용 가방)로 아이템 추가 시도.  
**공간 판정:**

* **공간 있음:** 정상적으로 가방에 들어감.  
* **공간 없음 (Full):**  
  * **\[임시 보관함(Temporary Stash)\]** UI 팝업 생성.  
  * 플레이어는 기존 아이템을 버리거나, 회수된 장비를 포기해야 함.

**소실 타이머 (Expiration):**

* 이 임시 보관함 상태는 \*\*\[다음 유닛의 턴 종료 시점\]\*\*까지 유지됨.  
* 이때까지 정리하지 않으면 임시 보관함에 남은 아이템은 **영구 소실(Destroy)** 처리됨.

#### **8\. 스킬/사거리 계산 (Chebyshev)**

* **유클리드 거리(Euclidean Distance) 사용. 두 점 사이의 '직선 거리'를 기준으로 원형의 사거리를 계산한다. (예: 반경 5칸 이내)**.  
* 로직: 스킬/사거리 \`R\` 내에 목표 \`B(x2, z2)\`가 있는지 판단할 때, 공격자 \`A(x1, z1)\`로부터의 거리 \`D\`가 \`R\` 이내인지 확인한다. 성능 최적화를 위해 제곱 거리(\`D^2\`)를 사용하여 비교한다.  
  


### **6.4. 데이터 구조 (Data Architecture)**

모든 기획 데이터는 수정이 용이한 ScriptableObject로 구조화한다.

| 데이터 타입 (SO) | 포함 필드 (Fields) | 용도 |
| :---- | :---- | :---- |
| **UnitDataSO** | 이름, 병과, 모델 프리팹, BaseHP, Mobility, Accuracy, Evasion | 아군/적군 유닛의 기본 스펙 정의. |
| **WeaponDataSO** | 이름, 사거리, 데미지, **ActionModule (참조)** | 무기별 스펙 및 공격 방식 정의. |
| **ActionModuleSO** | 미니게임 타입(Hold, Timing, Focus), 난이도 계수, UI 프리팹 | 무기마다 다른 액티브 게이지 로직을 모듈화. |
| **MapDataSO** | 맵 프리팹(Terrain), 적 스폰 포인트 리스트, 등장 적 종류(Pool) | 미션별 맵 구성 및 적 배치 정보. |
| **LootTableSO** | 아이템 리스트, 드랍 확률(Weight) | 적 사망 시 드랍할 아이템 테이블. |
| **ConsumableDataSO** | 이름, 효과 타입(Damage/Heal/Buff/Scan), 효과 수치, 범위, 구매 가격, 환불 가격 | 소모품 스펙 정의 |

### **6.5. 코딩 컨벤션 (Coding Conventions)**

Enum 관리 원칙: '1 Enum, 1 File'

* 모든 열거형(Enum)은 각자의 의미를 가장 잘 나타내는 이름의 .cs 파일에 단독으로 정의한다. (예: CoverType.cs, UnitCondition.cs, PlayerActionState.cs)  
* 목적:\* 프로젝트의 규모가 커지더라도 코드의 명확성과 유지보수성을 유지하고, 여러 개발자가 동시에 작업할 때 발생할 수 있는 병합 충돌을 원천적으로 방지하기 위함이다. 이는 프로젝트 전반의 '관심사 분리' 설계 원칙과 일관성을 유지한다.  
* "모든 비동기 메서드에는 접미사 `Async`를 붙이지 않더라도 반환 타입으로 구분한다(UniTask). `void` 반환 비동기 메서드(`async void`)는 이벤트 핸들러를 제외하고 금지하며, `async UniTaskVoid`를 사용한다."

### **6.6. 유닛-컨트롤러 실행 구조 (Implementation)**

| 클래스명 | 타입 | 주요 역할 | 설명 |
| :---- | :---- | :---- | :---- |
| **PlayerUnitDataSO** | ScriptableObject | 기본 스펙 정의 | 유닛의 변하지 않는 기본 데이터 (최대 HP, 기본 스탯, 병과 등)를 정의하는 데이터 컨테이너. |
| **EnemyUnitDataSO** | ScriptableObject | 기본 스펙 정의 | 유닛의 변하지 않는 기본 데이터 (최대 HP, 기본 스탯, 병과 등)를 정의하는 데이터 컨테이너. |
| **Unit.cs** | MonoBehaviour | 상태 관리 및 행동 주체 | 전장에 스폰되는 모든 유닛 게임 오브젝트에 부착. \`UnitDataSO\`를 참조하여 자신의 상태를 초기화하며, 현재 HP, 위치, 버프/디버프 등 실시간으로 변하는 상태를 관리. \`MoveTo()\`, \`TakeDamage()\` 등 외부의 명령을 받아 행동을 '실행'하는 역할. |
| **PlayerController.cs** | 일반 C\# 클래스 | 판단 및 명령 주체 (플레이어) | \`TurnManager\`에게 턴을 부여받으면 활성화. 유저 입력을 받아 상황을 '판단'하고, 자신이 조종하는 \`Unit\` 인스턴스에게 \`Attack()\`, \`MoveTo()\` 같은 명령을 '지시'하는 두뇌 역할. |
| **AIController.cs** | 일반 C\# 클래스 | 판단 및 명령 주체 (AI) | \`TurnManager\`에게 턴을 부여받으면 활성화. AI 로직에 따라 상황을 '판단'하고, 자신이 조종하는 \`Unit\` 인스턴스에게 \`Attack()\`, \`MoveTo()\` 같은 명령을 '지시'하는 두뇌 역할. |

\*\*실행 흐름:\*\*

    1\.  \`UnitManager\`가 \`UnitDataSO\`를 기반으로 \`Unit\` 프리팹을 전장에 스폰한다.

    2\.  \`TurnManager\`가 다음 턴 유닛을 결정하고, 해당 \`Unit\`의 컨트롤러(\`PlayerController\` 또는 \`AIController\`)를 활성화한다.

    3\.  컨트롤러는 입력을 받거나 스스로 판단하여 \`Unit\`에게 행동을 명령한다.

    4\.  \`Unit\`은 명령에 따라 행동(애니메이션, 이동, 상태 변경 등)을 수행한다.

# **7.0. UI/UX 시스템 (User Interface & Experience)**

### **7.1. 디자인 원칙 (Design Principles)**

* **분산형 인터페이스 (Decentralized UI):** *XCOM 2* 스타일을 차용하여, 정보의 성격에 따라 화면 좌/우/중앙으로 UI를 분산 배치하여 전장(Center Screen)의 시야를 확보한다.  
* **상황 반응형 연출 (Context-Sensitive):** 평소에는 전술 정보를 보여주다가, 액션(공격/방어) 순간에는 UI를 숨기고 QTE 패널과 카메라 연출에 집중하게 하여 몰입감을 높인다.  
* **직관적 피드백:** 복잡한 수치 계산(엄폐, 각도) 결과는 \*\*'액티브 게이지의 영역 크기'\*\*와 \*\*'명중률 %'\*\*로 단순화하여 표시한다.

---

### **7.2. 전투 화면 레이아웃 (Battle HUD)**

| 위치 | 구성 요소 (Component) | 표시 정보 및 기능 |
| :---- | :---- | :---- |
| **좌측 하단**  (Unit Status) | **유닛 정보 패널** | • **초상화:** 현재 턴 유닛의 얼굴. • **HP 바:** 현재 체력 / 최대 체력 (수치 포함). • **상태 아이콘:** 버프/디버프 (마우스 오버 시 툴팁). |
| **우측 하단**  (Weapon Info) | **무기 패널** | • **무기 이미지:** 현재 장착 중인 주무기. • **기본 스펙:** 공격력 범위 (ex: 4-6), 치명타 확률. |
| **하단 중앙**  (Main Deck) | **공용 인벤토리 및 스킬 패널** | • **1열(하단):** \[원정대 가방\] 슬롯. 보유 중인 탄약 및 소모품 리스트 표시 (Drag & Drop 지원). • **2열(상단):** 현재 유닛의 \[액티브 스킬\] 버튼 나열 (사격, 경계, 병과 스킬 등).  |
| **타겟 상단**  (World Space) | **타겟 정보** | • **HP 바:** 적의 남은 체력. • **방어 상태:** 엄폐 아이콘 (방패 모양 \- 깨짐/반/완전). • **명중률:** 최종 계산된 Hit Chance (%). |
| **화면 중앙**  (Overlay) | **QTE 패널** | • **공격 시:** 무기별 액티브 게이지 (Attack Phase에만 팝업). • **방어 시:** 위기 감지 경고 및 커맨드 입력창 (Defense Phase에만 팝업). |
| **공격 활성화 유닛 주변**  | **공격 사거리 표시 원** | World Distance(반경, Radius)로 계산하며, 이를 시각적으로 보여주는 **Decal/Gizmo** 시스템이 필요함 |

---

### **7.3. 전투 조작 흐름 및 카메라 연출 (Combat Flow)**

**(1) 공격 시퀀스 (Player Attack Sequence)**

1. **유닛 포커스 (Turn Start):**  
   * 현재 턴인 아군 유닛 **A**에게 카메라 포커스 및 조작 권한 부여.  
2. **타겟 지정 (Targeting):**  
   * 플레이어가 사거리 내의 적 유닛 **B**를 클릭 (또는 패드 타겟팅).  
   * 적 **B**의 머리 위에 **\[타겟 정보(명중률, HP)\]** 표시.  
3. **행동 선택 (Select Skill):**  
   * 하단 중앙 \*\*\[스킬 바\]\*\*가 활성화됨 (이전까진 비활성 혹은 숨김).  
   * 마우스 클릭, 숫자키(1\~9), 혹은 패드 조작으로 사용할 스킬 선택.  
   * *선택 시 스킬의 예상 데미지나 효과 범위가 미리보기(Preview) 됨.*  
4. **타겟 선택 및 공격 확정 (2-Step Confirmation):**  
   * (PC/모바일 공통) 공격 스킬 선택 후 적 유닛을 클릭(터치)하면, 공격이 즉시 발동한다.

      **5\. UI 전환 및 QTE 진입 (Enter Action Phase):**

* 공격이 확정되면, 기존 HUD가 사라지거나 흐려지고(Dimmed), 화면 정중앙에 \[무기별 미니게임 패널\] 팝업.  
  * 소모품 사용 시에는 QTE(미니게임) 없이 즉시 발동  
5. **QTE 수행 (Action):**  
   * 플레이어의 피지컬 조작 수행 (Timing / Hold / Focus).  
6. **결과 연출 (Result):**  
   * 액션 뷰(Action View) 진입.  
     1. 공격자와 타겟 사이의 특정 오프셋(`actionViewOffset`) 지점으로 카메라가 즉시 이동하여 박진감 있는 구도를 형성함.  
     2. 타격 시 \*\*임팩트 쉐이크(Impact Shake)\*\*를 호출하여 시각적 타격감을 극대화함  
   * 총구 화염, 탄환 궤적, 타격 이펙트 재생.  
   * **결과 확인:** 피격 모션, HP 감소, 데미지 텍스트(Critical/Miss)가 뜨는 동안 **약 1.0\~1.5초간 카메라 유지**.  
7. **카메라 복귀 (Return):**  
   * 턴이 종료되면 다음 순서 유닛에게로 이동.  
   * 턴이 남아있다면 다시 유닛 **A**에게로 복귀.

**(2) 방어 시퀀스 (Active Defense Sequence)**

1. **적 행동 개시:** 적 유닛 **B**가 행동을 시작. 카메라는 **B**를 비춤.  
2. **위기 감지 (Focus & Warning):**  
   * 적 **B**가 아군 **A**를 공격하려는 순간, 카메라가 공격자와 피격자를 한 화면에 담는 액션 뷰 구도로 전환  
   * **0.5초 대기:** 플레이어가 "내가 공격받는구나"를 인지할 시간 부여.  
3. **QTE 판정 및 발동 (Conditional QTE):**  
   * **조건 체크:** 이번 공격이 **\[치명타\]** 혹은 \*\*\[사망(Lethal)\]\*\*에 해당하는가?  
   * **True (위기):** **CalculateSurvivalChance() 확률로 경감 및 즉사 방지 QTE 발동. 성공 시 경감(20%) / HP 1로 생존하며, 실패 시 경감이 적용되지 않거나 사망발동.**"  
   * QTE 성공 시: **체력 1을 남기고 생존.**  
   * **다수 타겟 발생 시: 무작위(Random) 순서로 순차적(Sequential) QTE 진행.**  
   * **False (일반):** QTE 없이 바로 피격 연출로 진행.  
4. **결과 연출:**  
   * 아군 **A**의 피격/회피 모션 및 HP 변화.  
   * **1.0초 유지:** 플레이어가 피해량을 확인할 수 있도록 대기.  
5. **카메라 복귀:** 다시 행동권을 가진 적 유닛 **B** 혹은 다음 턴 유닛에게로 이동.

---

### **7.4. 액티브 게이지 시각화 (Visual Feedback)**

공격 QTE 패널은 무기 타입에 따라 다른 UI 프리팹을 사용한다.

| 무기 타입 | UI 디자인 컨셉 | 조작 방식 |
| :---- | :---- | :---- |
| **돌격소총** | **수평 게이지 (Horizontal Bar)**  왼쪽에서 오른쪽으로 차오르는 게이지. | **Hold & Release**  버튼을 누르면 게이지가 빠르게 상승 하강 반복, \[성공 영역\]에서 버튼을 뗌. |
| **저격소총** | **스코프 뷰 (Scope Circle)**  화면 중앙 십자선과 흔들리는 조준원. | **Focus Aim**  마우스/스틱으로 흔들리는 원을 중앙에 유지하며 시간 버티기. |
| **샷건/근접** | **원형 링 (Radial Ring)**  시계 방향으로 회전하는 바늘. | **Timing Hit**  바늘이 \[성공 영역\]을 지날 때 버튼 클릭 (리듬게임). |

---

### **7.5. 입력 매핑 (Input Mapping) \- *InputManager 연동***

| 동작 (Action) | PC (Keyboard/Mouse) | Gamepad (Xbox 기준) | 비고 |
| :---- | :---- | :---- | :---- |
| **카메라 이동** | WASD / 마우스 화면 가장자리 | 왼쪽 스틱 (L-Stick) | 모든 카메라 이동 및 회전에는 보간(Lerp/Slerp) 및 Smoothing(기본값 5f)이 적용되어 부드러운 화면 전환을 제공함. |
| **카메라 회전** | 마우스 우클릭 드래그(`mouseRotationSpeed`) 및 키보드 회전 축(`rotationSpeed`) 동시 지원. | 오른쪽 스틱 (R-Stick) | 모든 카메라 이동 및 회전에는 보간(Lerp/Slerp) 및 Smoothing(기본값 5f)이 적용되어 부드러운 화면 전환을 제공함. |
| **커서/타겟팅** | 마우스 포인터 | D-Pad (타겟 순환) |  |
| **확인/선택** | 좌클릭 / Space | A 버튼 |  |
| **취소/뒤로** | 우클릭 / ESC | B 버튼 |  |
| **스킬 선택** | 숫자키 1 \~ 9 | LB / RB (스킬 순환) |  |
| **QTE 조작** | Space (공통) / 마우스 | A 버튼 (공통) / 스틱 | 무기별 상이 |
| **정보 보기** | Alt (토글) | Y 버튼 | 타겟 상세 정보 |

# **8.0. 핵심 데이터 구조 (Core Data Structure)**

게임 내 모든 콘텐츠 데이터는 ScriptableObject로 정의하며, DataManager를 통해 로드 및 참조된다.

### **8.1. PlayerUnitDataSO (유닛 기본 정보)**

| 대분류 | 필드명 (Variable) | 데이터 타입 | 상세 설명 및 역할 |
| :---- | :---- | :---- | :---- |
| 1\. Identity | UnitID | string | 시스템 내부 식별 ID (예: Player\_Assault\_01). |
|  | UnitName | string | UI에 표시될 유닛 이름. |
|  | Role | Enum (ClassType) | 병과 (Assault, Sniper, Scout). |
| 2\. Visual | ModelPrefab | AssetReference | \[Hybrid\] 유닛의 3D 모델 프리팹 (Addressables). |
| 3\. Base Stats | MaxHP | int | 유닛의 최대 체력. |
| (구조체 내부) | Mobility | int | 1턴당 이동 가능한 타일 수. |
|  | Agility | int | 턴 대기 시간(TS) 계산용 민첩성 수치. |
|  | Aim | int | 기본 사격 명중률 (%). |
|  | Evasion | int | 피격 시 회피 확률 및 QTE 난이도 영향. |
|  | CritChance | float | 치명타 발생 기본 확률 (0.0 \~ 100.0). |
| 4\. Loadout | MainWeapon | WeaponDataSO | 기본 장착 주무기 데이터. |
|  | BodyArmor | ArmorDataSO | 기본 장착 방어구 데이터 (방어 등급 결정). |

### 

### **8.2. EnemyUnitDataSO (유닛 기본 정보)**

| 대분류 | 필드명 (Variable) | 데이터 타입 | 상세 설명 및 역할 |
| :---- | :---- | :---- | :---- |
| 1\. Identity | UnitID | string | 시스템 내부 식별 ID. |
|  | UnitName | string | UI 표시 이름. |
|  | EnemyType | Enum | 등급 구분 (Normal, Elite, Boss). |
| 2\. Visual | ModelPrefab | AssetReference | \[Hybrid\] 적군 모델 프리팹. |
|  | HitVFX | AssetReference | \[Hybrid\] 피격 시 발생하는 유닛 고유 이펙트 (피, 기계 파편 등). |
| 3\. Base Stats | MaxHP | int | 적군의 최대 체력. |
| (구조체 내부) | Mobility | int | 1턴당 이동 가능한 타일 수. |
|  | Agility | int | 민첩성 (턴 속도). |
|  | Aim | int | 기본 명중률. |
|  | Evasion | int | 기본 회피율. |
|  | CritChance | float | 기본 치명타 확률. |
| 4\. AI Logic | BaseAILevel | int | \[AI\] 유닛 본인의 지능 레벨 (1\~10). 딥러닝 스코어 가중치. |
|  | CommandAIBonus | int | \[AI\] 주변 아군에게 부여하는 지능 보너스. (음수 가능) |
| 5\. Reward | DropTable | LootTableSO | 사망 시 드랍할 아이템 및 확률 테이블. |

### 

### 

### **8.3. WeaponDataSO (무기 데이터)**

| 대분류 | 필드명 (Variable) | 데이터 타입 | 상세 설명 및 역할 |
| :---- | :---- | :---- | :---- |
| **1\. Identity** | WeaponID | string | 무기 식별 ID. |
|  | WeaponName | string | 인벤토리/HUD 표시 이름. |
|  | WeaponIcon | Sprite | **\[Hybrid\]** UI 아이콘 (직접 참조). |
| **2\. Specs** | Type | Enum (WeaponType) | 무기 종류 (Rifle, Sniper, Shotgun). |
|  | AllowedClasses | List\<Enum\> | 장착 가능 병과 리스트. |
|  | Damage\_Min | int | (MinMaxInt 내부) 최소 데미지. |
|  | Damage\_Max | int | (MinMaxInt 내부) 최대 데미지. |
|  | Range | int | 최대 사거리 (타일 수). |
|  | CritBonus | float | 치명타 시 데미지 배율 (기본 1.5). |
| **3\. Ballistics** | AccuracyCurve | AnimationCurve | 거리(X)에 따른 명중률(Y) 변화 그래프. |
| **4\. VFX** | MuzzleVFX | AssetReference | **\[Visual\]** 발사 순간 총구 화염 이펙트. |
|  | TracerVFX | AssetReference | **\[Visual\]** 총알 궤적 이펙트 (Hitscan 표현용). |
|  | ImpactVFX | AssetReference | **\[Visual\]** 탄착 지점 폭발/피격 이펙트. |
| **5\. Action Logic** | **ConstraintType** | Enum | **행동 제약 유형.** (아래 설명 참조) • **Standard:** 이동 후 사격 가능. • **Heavy:** 이동 후 사격 불가 (고정 사격). |
|  | **EndsTurn** | bool | **턴 강제 종료 여부.**  • **True:** 사격 시 즉시 턴 종료 (소총, 저격총). • **False:** 사격 후에도 행동 가능 (권총). |
|  | **ActionModule** | AssetReference | **\[Logic\]** 공격 QTE 미니게임 로직 모듈. |

### **8.4. ArmorDataSO (방어구 데이터)**

아군 대원 전용 방어구.

* **수정사항:** MobilityPenalty로 명칭 변경. 입력된 수치만큼 이동력이 **감소**함 (과학적 고증 반영).

| 필드명 | 타입 | 설명 |
| :---- | :---- | :---- |
| **ArmorID** | string | 방어구 ID |
| **DefenseTier** | int | 방어 등급 (T1\~T5) |
| **MobilityPenalty** | int | **이동력 감소량**. (예: 1 입력 시 이동력 \-1) |

### 

### **8.5. ConsumableDataSO (소모품 및 탄약 데이터)**

전투 중 사용하는 아이템 및 탄약. 캠프 구매/환불 데이터 포함.

#### **8.5.1 ItemDataSO (기본 아이템 정보) 모든 아이템의 공통 부모 클래스**

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **ItemID** | string | 시스템 식별 ID (예: ITEM\_Ammo\_T1, ITEM\_MedKit\_S) |
| **ItemName** | string | 인벤토리 및 툴팁에 표시될 이름 |
| **ItemIcon** | Sprite | \[Hybrid\] UI 아이콘 이미지 |
| **ItemType** | Enum | 아이템 대분류 (Ammo, Consumable, Resource) |
| **Description** | string | 아이템 설명 텍스트 |
| **Price\_Buy** | int | 상점 구매 가격 |
| **Price\_Sell** | int | 상점 판매 가격 (기본값: 구매가의 30%) |
| **MaxStack** | int | 한 슬롯에 겹칠 수 있는 최대 수량 (탄약/재료: 999, 장비: 1\) |

#### **8.5.2 AmmoDataSO (탄약 데이터) `ItemDataSO`를 상속받음. 전투 효율(Efficiency) 공식의 핵심 변수를 포함한다.**

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **AttackTier** | int | **공격 등급 (T1\~T5).** 방어구 등급(TDef)과의 격차 계산에 사용됨. |
| **AllowedWeaponTypes** | List\<WeaponType\> | 이 탄약을 사용할 수 있는 class군 (예: Rifle, Sniper) |
| \- `StatusEffects` |  |  |

#### **8.5.3 ConsumableDataSO (사용형 소모품 데이터) `ItemDataSO`를 상속받음. 액티브 스킬처럼 사용되는 아이템.**

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **EffectType** | Enum | 발동 효과 정의 (Heal, Buff\_Stat, Cure\_Status, Zone\_Create, Scan) |
| **EffectValue** | float | 효과의 강도 (회복량, 버프 수치 등) |
| **Duration** | int | 지속 턴 수 (즉발성 효과는 0\) |
| **VFX\_Ref** | AssetReference | 사용 시 재생할 이펙트 |

#### **8.5.4 전투 보상으로 획득하거나 필드에서 루팅하는 비소비성 아이템. `ItemDataSO`를 상속받아 구현한다.**

전투 보상으로 획득하거나 필드에서 루팅하는 비소비성 아이템. 기지 업그레이드 및 제작 재료로 사용된다.

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **(Inherited)** | \- | ItemID, ItemName, ItemIcon, Description, Price\_Sell, MaxStack 상속 |
| **IsAutoSell** | bool | **자동 환불 여부.**  • **True:** 환금 아이템. 전투 종료 시 Price\_Sell 가격으로 즉시 판매. • **False:** 재료 아이템. 기지 창고(Inventory)로 이송되어 보관. |

### **8.6. QTEActionModuleSO (액션 기믹 모듈)**

무기별 미니게임 로직.

| 필드명 | 타입 | 설명 | 확정값 |
| :---- | :---- | :---- | :---- |
| **ScrollSpeed** | float | 게이지 이동 속도 (초당 1회 왕복 기준) | **1.0** |
| **Timeout** | float | 입력 제한 시간 (초) | **5.0** |
| **IsPingPong** | bool | 왕복 운동 여부 | **true** |
| **IsRandomPos** | bool | 성공 영역 랜덤 배치 여부 | **true** |
| **BasePrecision** | float | 성공 영역 내 정타 비율 (0\~1) | **0.7** |

### 

### **8.7. AbilityDataSO (어빌리티 및 스킬 데이터)**

스킬과 아이템 효과를 정의하는 데이터 구조.

#### **8.7.1. EffectType (효과 유형 정의) 스킬이 발동되었을 때 실행할 로직의 종류를 결정한다.**

| 타입명 (Enum) | 설명 및 로직 | 필요한 파라미터 예시 |
| :---- | :---- | :---- |
| Damage | 대상에게 직접 피해를 입힘 | Value(피해량), DamageType(물리/폭발) |
| Heal | 대상의 HP를 회복시킴 | Value(회복량) |
| Buff\_Stat | 대상의 스탯(이동력, 명중률 등)을 일시적으로 강화 | StatType, Value, Duration(지속턴) |
| Debuff\_Stat | 대상의 스탯을 감소시킴 | StatType, Value, Duration |
| Cure\_Status | 특정 상태이상(출혈, 중독 등)을 제거 | TargetStatus(제거할 상태) |
| Apply\_Status | 대상에게 상태이상(출혈, 화상, 기절) 부여 | TargetStatus, Duration, Chance(확률) |
| Zone\_Create | 지면에 장판(화염, 산성 등)을 생성 | ZoneID, Duration, Radius |
| Scan\_Area | 지정 범위의 안개(FOW)를 제거하고 은신 유닛 감지 | Radius, Duration |
| Reload | 탄약을 교체하거나 재장전함 | AmmoID (Optional) |

#### 

#### **8.7.2 AbilityEffect 구조체 (단일 효과 데이터) `AbilityDataSO`는 이 구조체의 리스트(`List<AbilityEffect>`)를 가져, 하나의 스킬이 여러 효과(예: 데미지 \+ 출혈)를 동시에 주도록 구성한다.**

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **Type** | EffectType | 위의 Enum 값 중 하나 선택 |
| **Value** | float | 효과의 강도 (데미지, 회복량, 버프 수치) |
| **Duration** | int | 지속 턴 수 (즉발 효과는 0\) |
| **Radius** | int | 효과 범위 (0: 단일 타겟, 1 이상: 광역) |
| **StatusRef** | StatusEffectSO | \[참조\] 적용하거나 해제할 상태이상 데이터 |
| **ZoneRef** | ZoneDataSO | \[참조\] 생성할 장판 데이터 (화염병 등) |

### **8.8. MapDataSO (맵/미션 데이터)**

| 필드명 (Field Name) | 데이터 타입 (Data Type) | 설명 (Description) |
| :---- | :---- | :---- |
| **MapID** | string | 맵을 시스템 내부에서 식별하는 고유 ID. |
| **DisplayName** | string | 플레이어에게 UI 상으로 노출되는 맵의 이름. |
| **GridSize** | Vector2Int | 맵의 가로(X)와 세로(Z) 크기. |
| **Min/MaxLevel** | int | 맵의 유효한 층수 범위 (Y축). |
| **MapPrefabRef** | AssetReference | 맵의 배경이 되는 3D 환경 모델 프리팹 (Visual Only). |
| **Tiles** | List\<TileSaveData\> | **\[희소 배열\]** 실제 데이터(바닥, 벽, 기둥)가 존재하는 타일 정보 리스트. |
| **TryGetRoleLocation** | (Method) | **특정 `RoleTag`를 가진 타일의 좌표를 검색하는 기능을 내장하여 별도의 스폰 포인트 리스트 관리를 대체함** |

### **8.9. MapEditorSettingsSO (맵 에디터 공통 설정)**

| 필드명 | 데이터 타입 | 설명 |
| :---- | :---- | :---- |
| **FloorTable** | List\<FloorMapping\> | 바닥 재질(FloorType) $\\leftrightarrow$ 프리팹 매핑 리스트. |
| **PillarTable** | List\<PillarMapping\> | 기둥 종류(PillarType) $\\leftrightarrow$ 프리팹 매핑 리스트. |
| **WallTable** | List\<WallMapping\> | 벽 종류(EdgeType) $\\leftrightarrow$ 프리팹 매핑 리스트. |
| **EditorMaterials** | Material\[\] | 에디터 전용 시각화 재질 (선택된 타일 하이라이트, 층별 반투명 처리용). |

### 

### **8.10. StatusEffectSO**

#### **1\. 분류 및 로직**

* **Injury (부상):** 일반 탄약 피격 시 물리적 충격으로 발생. 자연 회복 불가.  
* **Debuff (디버프):** 일시적 컨디션 저하.  
* **System (시스템):** 기계적/신경학적 오류.

#### **2\. 상태이상 목록 (Status List)**

| 분류 | 상태명 (ID) | 효과 및 패널티 (Logic) | 치료/해제 (Cure) | 발생 원인 |
| :---- | :---- | :---- | :---- | :---- |
| **부상** | **출혈 (Bleeding)** | • 매 턴 종료 시 **HP 2** 피해.  | 붕대 (Bandage) | 일반 피격 |
| **부상** | **과다출혈 (Heavy Bleed)** | • 매 턴 종료 시 **HP 2** 피해. • 이동 시 이동한 타일 \* 3만큼 피해. | 지혈대 (Tourniquet) | 출혈탄 일반 피격 |
| **부상** | **팔 골절 (Fracture\_Arm)** | • **최종 명중률(Aim) \-30%**. • 수류탄 투척 사거리 반감. | 부목 (Splint) | 폭발 / 일반피격 |
| **부상** | **다리 골절 (Fracture\_Leg)** | • **이동력 \-50%**. • **회피율(Evasion) 0%** 고정 (방어 QTE 불가). | 부목 (Splint) | 폭발 / 낙하 / 일반피격 |
| **디버프** | **진통 (Pain)** | • **치명타 확률-10% 합연산** (집중력 상실). • 방어 QTE 난이도 상승 (Evasion \-10). | 진통제 (Painkillers) (사용시 x턴 동안 진통 효과 무효) | 종류 상관 없이 피격 |
| **특수** | **화상 (Burn)** | • 매 턴 **HP 3** 피해.  | 화상 연고 / 물 | 화염수류탄 / 소이탄 |
| **시스템** | 차단 주파수 초과 (Cutoff Freq Exceeded) | • **\[Signal Attenuation\]:** 신호 감쇠로 NS 회복 불가. • 매 턴 **NS \-10** (지속적 데이터 손실). | 위상 정류기 (Phase Rectifier) | 플라즈마탄 / 플라즈마폭탄 |

# **9.0. 핵심 밸런싱 공식 (Core Balancing Formulas)**

본 장에서는 8.0장의 데이터(SO)들이 실제 게임 내에서 상호작용하여 결과를 도출하는 수학적 규칙을 정의한다.

### **9.1. 데이터 처리 원칙 (Data Processing)**

* **내부 연산 (Logic):** 모든 데미지와 확률 계산은 float 형태로 처리하며, **소수점 셋째 자리에서 반올림하여 둘째 자리까지** 유지한다.  
  * *예:* 5.678 → 5.68  
* **UI 표시 (Display):** 플레이어에게 보여주는 최종 데미지 및 HP 수치는 \*\*소수점을 버리고 정수(자연수)\*\*로만 표기한다.  
  * *예:* 내부 5.68→ UI 표시 5

### **9.2. 데미지 산출 파이프라인 (Damage Pipeline)**

1) 공격 적중 시, 최종 데미지는 다음 단계의 연산을 거쳐 결정된다.

FinalDamage \= BaseDmg × fdmg (d)  × Efficiency × CritMod

fdmg(d) : 무기별 damageFolloffCurve에서 산출된 배율

* **결과 처리:** 최종값은 **\[5, 99\]** 범위로 클램프(Clamp)함.

| 단계 | 변수명 | 설명 및 공식 |
| :---- | :---- | :---- |
| **1\. 기본 피해** | BaseDmg | WeaponDataSO의 Min\~Max 범위 내 랜덤 추출. |
| **2\. 거리 보정** | RangeMod | DmgFalloff.Evaluate(Distance) (0.00 \~ 1.00). |
| **3\. 공방 효율** | Efficiency | 탄약 공격 등급과 방어 등급 차이에 따른 보정 (9.3항). |
| **4\. 치명타** | CritMod | **기본 1.5배**. (추후 스킬/아이템에 의해 합연산으로 증가 가능). |
| **5\. 최종 처리** | Result | 계산 결과는 소수점 둘째 자리까지 유지하되, UI에는 정수로 표시. |

2) 개념: 데미지 계산 전체 과정을 하나의 거대한 함수로 만들지 않고, 각 계산 단계를 독립적인 '데미지 조율기(Damage Modifier)' 부품으로 분리한다. CombatManager는 이 조율기들을 순서대로 실행시키는 컨베이어 벨트 역할을 한다  
   구현: 모든 '데미지 조율기' 클래스는 IDamageModifier 인터페이스를 구현하며, CombatManager는 List\<IDamageModifier\>를 통해 파이프라인을 구성하고 실행한다.  
   // 모든 데미지 조율기가 구현해야 하는 인터페이스  
   public interface IDamageModifier  
    {  
   // currentDamage: 이전 단계까지 계산된 데미지  
    // context: 공격자, 방어자 등 모든 전투 정보가 담긴 객체  
   float Apply(float currentDamage, CombatContext context);  
   }  
   최종 데미지 공식:  
         FinalDamage \= BaseDmg × RangeMod × Efficiency × CritMod  
         (각 변수는 아래 파이프라인 단계에서 순차적으로 계산 및 적용됨)  
   파이프라인 단계별 설명:  
   

| 순서 | 단계 (변수명) | 설명 및 공식 | 파이프라인 구현체 (예시) |
| :---- | :---- | :---- | :---- |
| **1** | **기본 피해**  (BaseDmg) | WeaponDataSO의 Min\~Max 범위 내 랜덤 추출. 이 값이 파이프라인의 초기 입력값이 됨. | BaseDamageCalculator |
| **2** | **거리 보정**  (RangeMod) | DmgFalloff.Evaluate(Distance) (0.00 \~ 1.00). 이전 데미지에 곱연산. | RangeDamageModifier |
| **3** | **공방 효율**  (Efficiency) | 탄약 공격 등급과 방어 등급 차이에 따른 보정 (9.3항). 이전 데미지에 곱연산. | TierEfficiencyModifier |
| **4** | **치명타**  (CritMod) | 기본 1.5배 (추후 스킬/아이템에 의해 합연산으로 증가 가능). 이전 데미지에 곱연산. | CriticalDamageModifier |
| **5** | **최종 처리**  (Result) | 최종값을 \[5, 99\] 범위로 제한(Clamp)하고, 소수점을 버려 정수로 변환. | FinalDamageClampModifier |

   

 


### **9.3. 공방 효율 공식 (Tier Efficiency)**

\*\*"탄약(공격) 등급과 방어구 등급의 격차(Gap)"\*\*에 따라 데미지 효율이 결정된다.

* **변수 정의:**  
  * **공격 등급 (**TAtk**):** ConsumableDataSO (Ammo)의 AttackTier.  
  * **방어 등급 (**TDef**):** 착용 방어구의 DefenseTier (적군은 UnitDataSO의 BaseDefTier).  
  * **격차 (**Gap**):** TDef-TAtk  
* 공식:  
  Efficiency \= 2/(max(0,Gap)+2)

### **9.4. QTE 시스템 상세 명세 (Specification)**

1\. 입력 및 조작 (Input)

* 입력 키: Space Bar 또는 LMB (마우스 좌클릭).  
* 조작 방식: 롱 프레스 (Hold & Release).  
  * 버튼을 누르고 있으면(Hold) 게이지 바늘이 이동한다.  
  * 버튼을 떼면(Release) 즉시 멈추고 해당 위치의 색상을 판정한다.  
* 제한 시간 (Timeout): 5초. 시간 내에 입력하지 않으면 자동 실패(Miss) 처리한다.  
* 예외 처리: QTE 진행 중에는 ESC 메뉴 호출 불가, 게임 일시정지 불가.

2\. 게이지 동작 (Gauge Behavior)

* 운동 방식: 등속 왕복 운동 (Ping-Pong).  
* 속도: 1.0 Hz (0 → 100 도달에 1초, 복귀에 1초). 상승/하강 속도 동일.  
* 수치 체계: 내부 로직은 0.0 \~ 1.0 (Normalized Float)을 사용하며, UI는 해상도에 맞춰 자동 스케일링된다.

3\. 영역 산출 및 배치 (Calculation & Layout)

* 성공 영역 크기: $5 \+ (HitChance \\times 0.65)$ (최소 5% \~ 최대 70%).  
* 내부 비율 (Precision): 성공 영역 내에서 \[정타(Yellow+Red) : 스침(Green)\] 비율은 7 : 3을 기본으로 한다.  
* 치명타 침식: 정타(Yellow) 영역의 지분을 치명타 확률(CritChance)만큼 빨강(Red)으로 교체한다.  
* 배치 (Random): 성공 영역은 게이지의 랜덤한 위치에 생성된다. (양 끝 5% 안전지대 제외).

4\. 판정 및 결과 (Result)

* 성공 (Red/Yellow/Green): 해당 구역의 데미지 배율 적용.  
* 실패 (Black): 빗나감 판정. 최종 데미지 0\.  
* 연출: 판정 즉시 결과 텍스트를 표시하고, 0.5초 후 UI를 닫는다.

### 

게이지 바는 4가지 색상 영역으로 구분되며, 각 영역에 히트(Hit)했을 때의 결과는 다음과 같다.

| 색상 (Color) | 판정 (Result) | 데미지 보정 (Multiplier) | 설명 |
| :---: | :---: | :---: | :---: |
| **검정 (Black)** | **빗맞음 (Miss)** | **0% (데미지 없음)** | 명중 실패. 탄환이 빗나감. |
| **녹색 (Green)** | **스치듯 명중 (Graze)** | **75% (데미지 감소)** | 명중했으나 급소를 피함. 일반 데미지의 75%만 적용. |
| **노랑 (Yellow)** | **정확한 명중 (Hit)** | **100% (기본 데미지)** | 의도한 대로 정확히 명중함. |
| **빨강 (Red)** | **치명타 (Critical)** | **150% \+ @** | 급소 적중. 치명타 보너스 적용. |

#### **(2) 영역 비율 산출 공식**

플레이어의 \*\*스탯(명중률, 치명타율)\*\*은 이 영역들의 \*\*'크기(너비)'\*\*를 결정하는 데 사용된다.

1. **전체 성공 범위 (Success Zone):**  
   * **공식:** TotalWidth \= (Unit.Aim \- CoverPenalty) / 100  
   * 이 범위가 \*\*\[녹색 \+ 노랑 \+ 빨강\]\*\*의 총합이 된다.  
2. **치명타 영역 (Red Zone):**  
   * **공식:** RedWidth \= YellowlWidth \* (Unit.CritChance / 100\)  
   * 성공 범위 내에서 치명타 확률만큼의 지분을 차지한다.  
3. **안정권 영역 (Yellow Zone):**  
   * **공식:** YellowWidth \= TotalWidth \* 0.3 (기본값)  
   * 숙련도나 장비에 따라 노란색 영역 비율을 늘려 '녹색(손해)'을 줄일 수 있다.  
4. **손해 영역 (Green Zone):**  
   * **공식:** GreenWidth \= TotalWidth \- (RedWidth \+ YellowWidth)  
   * 명중률은 확보했으나, 완벽하게 조준되지 않은 불안정한 영역.  
5. **StartPos:** `Random.Range(5, 95 - SuccessWidth)`  
6. `명중률 50% 크리티컬 50%의 경우 :` 

#### **(3) MVP 동작 및 판정 규칙**

MVP 단계의 돌격소총(Hold & Release) 및 공통 QTE 동작을 위한 필수 규칙을 정의한다.

1. 원샷 룰 (One-shot Rule):  
* QTE 입력은 \*\*단 1회의 기회\*\*만 주어진다.  
* 버튼을 누르는 순간(Press) 게이지가 움직이기 시작하며, 버튼을 떼면 다음 프레임에 확정이 되며 발사가 된다.  
* 중도 취소는 불가능하다.  
2. 왕복 속도 (Cursor Speed):  
*  기본 속도: 0에서 100 도달에 약 1초 (왕복 2초)  
* 변동 : 무기 종류나 \`ActionModule\` 설정에 따라 속도 계수(Multiplier)를 적용한다.  
* 움직임 : 등속 운동(Linear)을 기본으로 하되, 양 끝(0, 100)에서 즉시 방향이 반전(Bounce)된다.  
3. 타임아웃 (Timeout):  
* 제한 시간: QTE UI가 팝업된 후 5초  
* 결과:제한 시간 내에 플레이어가 입력을 완료(Release)하지 못하면 Miss 처리

### **9.5. 이동력 산출 공식 (Mobility Formula)**

FinalMobility \= max(1,Unit.Mobility \- Armor.MobilityPenaly)

### **9.6. 경제 밸런싱 (Economy)**

* **구매가:** `ConsumableDataSO` 데이터 참조.  
* **환불가:**

RefundCost \= \[PurchaseCost × 0.3 \] 소수점 버림

# **10.0. 데이터 기반 어빌리티 및 UI 시스템 (Data-Driven Ability & UI System)**

### **10.1. 아키텍처 목표 및 구현 가이드 (Architectural Goal & Implementation Guide)**

잘못된 구현 (Anti-Pattern): A안  
  이와 같은 방식의 구현을 엄격히 금지한다. 아래 코드는 UI가 게임의 모든 규칙을 알아야 하므로, 신규 콘텐츠 추가 시 UI  
  코드를 직접 수정해야 하는 문제를 야기한다.

    1 // A안: 잘못된 구현의 예시. (UI 코드 내)  
    2 function DrawUI\_Bad(unit) {  
    3     // 규칙 1: 4번 슬롯은 병과에 따라 다르다.  
    4     if (unit.Class \== "Assault") {  
    5         ShowIcon(4, "파편수류탄\_아이콘");  
    6     } else if (unit.Class \== "Sniper") {  
    7         ShowIcon(4, "화염병\_아이콘");  
    8     }  
    9     // ... 신규 병과가 추가될 때마다 이 if문을 계속 수정해야 함 ...  
   10  
   11     // 규칙 2: 5번 슬롯은 레벨에 따라 바뀐다.  
   12     if (unit.Level \< 5\) {  
   13         ShowIcon(5, "일반\_제압사격\_아이콘");  
   14     } else {  
   15         ShowIcon(5, "강화\_제압사격\_아이콘");  
   16     }  
   17     // ... 새로운 레벨업 규칙이 생기면 이 코드를 또 수정해야 함 ...  
   18 }

  \---  
  올바른 구현 (Target Architecture): B안  
  모든 UI 구현은 아래의 설계 사상을 따라야 한다. UI는 '어빌리티 목록'이라는 데이터의 '시각적 표현'일 뿐, 어떠한 게임  
  규칙도 포함해서는 안 된다.

    1 // B안: 올바른 구현의 예시. (UI 코드 내)  
    2 function DrawUI\_Good(unit) {  
    3     // 1\. '진실의 원천'인 현재 유닛의 어빌리티 목록을 가져온다.  
    4     List\<Ability\> abilities \= unit.GetAbilities();  
    5  
    6     // 2\. 목록을 순회하며 각 어빌리티 데이터가 시키는 대로 그린다.  
    7     foreach (Ability ability in abilities) {  
    8         // 이 어빌리티의 아이콘은? \-\> ability.Icon  
    9         // 이 어빌리티의 선호 슬롯은? \-\> ability.PreferredSlot  
   10         ShowIcon(ability.PreferredSlot, ability.Icon);  
   11     }  
   12  
   13     // 신규 병과, 신규 레벨업 스킬, 신규 아이템 스킬이 추가되어도  
   14     // 이 UI 코드는 단 한 줄도 수정할 필요가 없다.  
   15     // 단지 유닛의 GetAbilities() 목록에 데이터가 추가/변경될 뿐이다.  
   16 }

.

### **10.2. 기본 슬롯 레이아웃 및 선호 타입 (Default Slot Layout & Preferred Types)**

 AbilityDataSO가 가지는 SlotPreference의 종류와, 각 타입이 우선적으로 배치될 슬롯 번호의 기준은 아래와 같다.

### 

| 슬롯 | 기능 | 대상 | 비고 |
| :---- | :---- | :---- | :---- |
| **1** | **주무기 사격** | EquippedWeapon | 기본 공격. |
| **2** | **경계 (Overwatch)** | (공통) | 이동하는 적 반응 사격. |
| **3** | **엄폐 (Hunker)** | (공통) | 회피/방어 대폭 상승. 공격 기회 소모. |
| **4** | **투척 무기** | Class Grenade | 병과별 고유 수류탄 (아래 참조). |
| **5** | **특수 기술** | Class Special | 병과별 특수기 (제압, 권총, 은신). |
| **6** | **탄약 선택** | Ammo Selector | 현재 탄약 표시. (▲▼ 키로 인벤토리 탄약 교체) |
| **7** | **소모품** | Inventory Items | 소지한 주사기, 음료, 키트 등 자동 바인딩. |
| **8** | **화면 복귀** |  | **화면 복귀** |
| **9** | **턴 종료** |  | 턴 종료 |

### 

### 

### **10.3. 병과별 기본 어빌리티 목록 (Default Ability List by Class)**

  각 병과는 생성 시 아래의 기본 AbilityDataSO들을 자신의 '어빌리티 목록'에 포함한다.

   \* 돌격병 (Assault):  
       \* \`파편 수류탄\` (SlotPreference: Grenade)  
       \* \`제압 사격\` (SlotPreference: Special)  
   \* 저격병 (Sniper):  
       \* \`화염병\` (SlotPreference: Grenade)  
       \* \`권총 사격\` (SlotPreference: SecondaryWeapon)  
   \* 정찰병 (Scout):  
       \* \`스캔 수류탄\` (SlotPreference: Grenade)  
       \* \`은신\` (SlotPreference: Special)

### 

###   **10.4. UI 레이아웃 규칙 (UI Layout Rules)**

   1\. 슬롯 우선순위 배정: BattleUIManager는 유닛의 '어빌리티 목록'을 가져와, 각 어빌리티의 SlotPreference에 따라 10.2절의 표에 명시된 번호에 우선적으로 아이콘을 배치한다.  
   2\. 유동적 슬롯 채우기: SlotPreference가 없거나, 해당 번호 슬롯이 이미 차 있는 어빌리티들은 2, 3, 6, 7번 등 비어있는 슬롯 중 앞 번호부터 순서대로 채워진다.  
   3\. 가변적 슬롯 개수: 유닛의 어빌리티 개수가 9개 미만일 경우, 사용하지 않는 슬롯은 UI 상에서 비활성화(Grayedout)되거나 숨김 처리한다.

### **10.5.  탄약 및 공용 인벤토리 시스템 (Ammo & Shared Inventory)**

본 게임은 유닛 개개인이 탄약을 소지하지 않고, 부대 전체가 공유하는 **\[원정대 가방(Expedition Stash)\]** 시스템을 채택한다.

#### **1\. 원정대 가방 (Expedition Stash)**

* **개념:** 전투 중 화면 하단 중앙(Skill Deck 상단)에 위치하는 공용 보급고.  
* **관리:** `InventoryManager`가 관리하며, 전투 시작 시 플레이어가 보유한 모든 소모품과 탄약이 이곳에 로드된다.

#### **2\. 자동 장전 및 초기화 (Auto-Load Logic)**

* **전투 진입 시:** 모든 대원은 자신의 주무기에 맞는 탄약 중 \*\*가장 낮은 등급(Lowest Tier)\*\*의 탄약을 원정대 가방에서 자동으로 찾아 장착 상태로 시작한다.  
* **잔탄 부족:** 만약 가방에 호환되는 탄약이 하나도 없다면, 해당 대원은 '탄약 없음(No Ammo)' 상태가 되어 사격 관련 스킬이 비활성화된다.

#### **3\. 탄약 교체 및 보급 (Interaction: Drag & Drop)**

* **UI 조작:** 플레이어는 하단 UI의 탄약 아이콘을 드래그하여 전장의 유닛(자신 또는 인접한 아군)에게 드롭할 수 있다.  
* **비용(Cost):** 비용(Cost \- Attack Opportunity):  
  * 전투 중 탄약 교체는 \*\*'공격 행동(Attack Action)'\*\*과 동일하게 취급한다.  
  * 교체 즉시 해당 턴의 **공격 기회(Attack Chance)를 소모**하며, `HasAttacked = true` 상태가 된다.  
  * 따라서 \*\*'이동 후 교체'\*\*는 가능하지만, **'교체 후 이동'** 또는 \*\*'교체 후 사격'\*\*은 불가능하다 (턴 종료).  
* **시각적 피드백:**  
  * **장착 중(Equipped):** 현재 선택된 유닛이 사용 중인 탄약 아이콘은 녹색 테두리로 강조된다.  
  * **호환 불가(Incompatible):** 유닛의 무기와 호환되지 않는 탄약은 드래그 시 붉은색으로 표시되거나 드롭이 불가능하다.

### **10.6. 장비 내구도 시스템 (Equipment Durability)**

고티어 장비의 무분별한 학살 파밍을 방지하고, 경제적 리스크를 부여하기 위한 시스템.

**1\. 내구도 감소 규칙**

* **무기:** 발사/공격 1회당 내구도 **1 감소**.  
* **방어구:** 피격 1회당 내구도 **1 감소**.

**2\. 성능 저하 (Malfunction)** 내구도가 **최대치의 50% 이하**로 떨어지면 '기능 고장' 상태가 되어 심각한 페널티를 받는다.

| 장비 종류 | 페널티 효과 (50% 이하 시) |
| :---- | :---- |
| **무기** | **최종 명중률 \-20%** / **데미지 \-30%** / 치명타 불가 |
| **방어구** | **방어 등급(Tier) 1단계 하락** / 이동력 \-1 |

**3\. 파손 및 수리**

* **0 도달 시:** 아이템이 **\[파손됨\]** 상태가 되어 장착 및 사용이 불가능해짐 (인벤토리 공간만 차지).  
* **수리:** 기지의 \*\*\[수리공방\]\*\*에서 자원(Junk)과 자금을 소모하여 복구 가능. 티어가 높을수록 수리비가 기하급수적으로 비싸짐.

### 

# **11.0. 턴 행동 규칙 (Turn Action Logic)**

\*\*"쏘면 끝난다"\*\*는 대원칙 하에 병과별 예외를 둔다.

## **11.1. 액티브 턴 결정 공식 (TS: Turn Speed)**

모든 유닛은 고유의 **TS(Turn Speed)** 수치를 가지며, 이 수치가 0에 먼저 도달하는 순서대로 턴을 획득한다.

* **TS 산출 공식:** Final\_TS \= (Base\_Agility \* Random\_Weight) \+ NS\_Bonus \- Action\_Penalty  
  * **Base\_Agility:** 유닛의 기본 민첩성.  
  * **Random\_Weight:** 0.8 \~ 1.2 사이의 난수 (턴 순서의 가변성 부여).  
  * **NS\_Bonus:** **뉴럴 싱크(Neural Sync)** 수치가 높을수록 턴 대기 시간이 짧아지는 보너스 부여.  
  * **Action\_Penalty:** 직전 턴에서 수행한 행동의 무게 (아래 11.2 참조).

## **11.2. 행동별 페널티 (Action Cost / Penalty)**

행동을 적게 남길수록 다음 턴이 돌아오는 속도가 늦어진다.

| 수행 행동 | TS 페널티 (예시) | 비고 |
| :---: | :---: | :---: |
| **대기 (Wait)** | **\-10** | 아무것도 안 하고 턴 종료 시 가장 빠르게 복귀. |
| **이동만 수행** | **최대 \-40** | **상대적 피로도** 적용. `(기본 페널티10 + (사용한 이동력 / 최대 이동력)*30` 이는 유닛의 한계치에 가까운 이동을 할수록 다음 턴이 기하급수적으로 늦어짐을 의미함. |
| **공격만 수행** | **\-60** | 이동 없이 제자리 사격 시. |
| **이동 \+ 공격** | **\-100** | 모든 행동 기회 소진 시 가장 늦게 복귀. |
| **아이템 사용** | **\-30** | 소모품 종류에 따라 차등 적용. |
| **피격 시** | **이번 턴에 적용된 FinalTSpenalty의 10%/20%** | 일반 피격시/크리티컬 피격시 즉시 TS 누적 이동이라는 개념에 상당한 패널티를 주어 이동을 했음에도 제대로 된 곳으로 이동하지 못하였을 경우, 커다란 패널티를 입힘. 또한 일부러 턴을 넘기는 등의 전략적 플레이도 가능 내 턴에 피격시 즉시 TS를 깎지 않고 `Pool`에 누적 → **EndTurn() 호출 시점에 총합을 계산**하여 다음 턴 TS 배치에 반영. |

## 

## **11.3. 턴 스케줄러 로직 (Scheduler Logic)**

1. **계산:** 유닛의 행동이 종료되는 즉시 Action\_Penalty를 적용하여 Next\_Turn\_Tick을 계산한다.  
2. **정렬:** TurnManager는 모든 유닛을 Next\_Turn\_Tick이 낮은 순서(오름차순)로 실시간 재정렬한다.  
3. **호출:** 가장 상단에 있는 유닛에게 조작 권한을 부여한다.  
   * 

## **11.4. 무기별 행동 제약 규칙 (Weapon Action Constraints)**

행동 제약은 유닛의 병과(Class)가 아닌, \*\*현재 들고 있는 무기의 데이터(`WeaponDataSO`)\*\*에 의해 결정된다.

| 제약 유형 (Constraint) | 데이터 설정값 | 행동 규칙 (Sequence) | 해당 무기 예시 |
| :---- | :---- | :---- | :---- |
| **표준형 (Standard)** | Constraint: Standard  EndsTurn: True | • **이동 → 사격:** 가능 (턴 종료) • **사격 → 이동:** 불가 | 돌격소총(Rifle), 산탄총(Shotgun) |
| **중화기형 (Heavy)** | Constraint: Heavy  EndsTurn: True | • **이동 → 사격:** **불가** (이동 시 사격 비활성) • **사격 → 이동:** 불가 | 저격총(Sniper), 중기관총(MG) |
| **경량형 (Light)** | Constraint: Standard  EndsTurn: False | • **이동 → 사격:** 가능 • **사격 → 이동:** **가능**  | 권총(Pistol), SMG |

---

**설계 의도:** 병과별 특성은 "해당 병과가 어떤 무기를 주로 장착하는가"로 자연스럽게 구현된다. 예를 들어, 저격병은 `Heavy` 속성의 저격총을 들기에 이동 후 사격이 불가능한 것이지, 저격병이라는 코드 때문에 불가능한 것이 아니다.

## **11.5. 타임라인 UI 및 피드백 (Timeline & Delay)**

* **실시간 타임라인:** 화면 중단(또는 우측)에 '기준선(Center Line)'이 있고, TS 수치에 따라 좌/우(또는 상/하)로 초상화가 흘러가는 방식. 유닛의 `TS` 점수가 낮을수록 상단(다음 턴 예정)에 위치함.  
* 초상화 **Long Press(누르고 있기)** 시 해당 유닛으로 카메라 포커스 이동   
*  **개인 식별 시스템:**  
  * **배경색: 아군(Blue), 적군(Red)으로 피아 식별.**  
  * **식별자: 초상화 내 텍스트(P1, E1 등) 표시.**  
  * **하이라이트: 타임라인 초상화 마우스 오버/클릭 시 전장의 해당 유닛 발밑에 인디케이터 활성화.**  
* **턴 밀기 (Turn Delay):** **턴 주인공 여부**에 따라 차등 적용.  
  * 내 턴이 아닐 때 피격: 즉시 타임라인 순서가 밀림.  
  * 내 턴일 때 피격(경계 사격 등): 현재 행동은 유지하되, 다음 턴의 복귀 시간이 늦어짐.

# **12.0. 소모품 사용 로직 (Consumable Logic)**

### **12.1. 아이템 상세 목록 (Item List)**

모든 아이템은 공용 인벤토리(원정대 가방)에서 관리되며, 사용 시 효과는 다음과 같다.

| 분류 | 아이템명 | 대상 | 효과 및 로직 | 비고 |
| :---- | :---- | :---- | :---- | :---- |
| **강화** | **스팀팩 (Stimpack)** | 아군 | • **\[Buff\]** 3턴간 Mobility \+2, Aim \+10. (중첩 시 지속시간 갱신) |  |
| **치료** | **붕대 (Bandage)** | 아군 | • **\[Cure\]** Bleeding(출혈) 상태 제거 \+ HP 2 회복. |  |
|  | **Mark-1 (해독제)** | 아군 | • **\[Cure/Immunity\]** Poison(중독) 제거. 미리 사용 시 3턴간 면역. |  |
|  | **V4 (안정제)** | 아군 | • **\[Cure\]** Panic / Affliction 상태이상 제거. (NS 수치 회복 아님) | 상태이상만 해제 |
| **탄약** | **Standard Ammo (T1)** | 자신 | • **\[AttackTier\]** 1\. (가장 기본 탄약) | 방어구 T1 상대로 효율 100% |
|  | **High-Velocity (T2)** | 자신 | • **\[AttackTier\]** 2\. | 방어구 T2 상대로 효율 100% |
|  | **Armor-Piercing (T3)** | 자신 | • **\[AttackTier\]** 3\. | 방어구 T3 상대로 효율 100% |
|  | **... (T4\~T5)** | 자신 | • **\[AttackTier\]** 4\~5. | 고티어 적 대응용 |
| **투척** | **파편 수류탄** | 적/지형 | • **\[Damage\]** 범위 내 적에게 피해 \+ **벽 및 기둥 파괴 \+ 바닥 타일 파손(이동 비용 증가)**. | **Assault 전용** |
|  | **화염병** | 지형 | • **\[ZoneDamage\]** 범위(Radius 1.5)에 3턴간 화염지대 생성. | **Sniper 전용** |
|  | **스캔 수류탄** | 지형 | • **\[Scan\]** 범위(Radius 5.0) 내 시야 확보 및 은신 유닛 감지. | **Scout 전용** |

### 

# **13.0. 특수 게임플레이 시스템 (Special Gameplay Systems)**

전투의 변수와 깊이를 더하는 추가적인 심리 및 지휘 시스템

## **13.1. \[13.1. Neural Sync & Survival System\]**

* 개요: 유닛의 정신적 동기화 수준을 나타내는 지표. **0 \~ 200**의 범위를 가지며 기본값은 100이다.  
* 기존 '사기' 시스템을 '뉴럴 싱크'로 명칭 변경 및 데이터 통합.  
* 변동 요인:  
  * 감소: 아군/동료 사망, 치명타 피격, 적 지휘관의 특수 기술 피격 시.  
  * 회복: 적 사살, 치명타 공격 성공, 아군 지휘관의 격려 기술 사용 시.  
* **상태 구간 (Sync Thresholds)** NS 수치에 따라 유닛의 상태(Condition)와 전투 효율이 실시간으로 변동한다.

| 구간 (Range) | 상태명 (Condition) | 효과 (Effect) | 비고 |
| :---- | :---- | :---- | :---- |
| **180 \~ 200** | **희망 (Hopeful)** | **TS 20% 단축** / 생존율 x1.5 | 최상의 전투 효율 |
| **150 \~ 179** | **고무 (Inspired)** | **TS 10% 단축** / 생존율 x1.2 | 보너스 구간 |
| **50 \~ 149** | **정상 (Normal)** | 없음 | 기본 상태 |
| **35 \~ 49** | **무력화 (Incapacitated)** | **행동 불가** / 생존율 x0.8 | 턴을 쉬며 회복 대기 |
| **20 \~ 34** | **도주 (Fleeing)** | **통제 불능** / 생존율 x0.5 | 맵 밖으로 이동 시도 |
| **5 \~ 19** | **광란 (FriendlyFire)** | **피아식별 불가** / 생존율 x0.2 | 가장 가까운 대상 공격 |
| **0 \~ 4** | **자해 (SelfHarm)** | **자해 데미지** / 생존율 0 | 스스로 피해를 입힘 |

* 싱크로 펄스 (Synchro Pulse \- Overclock)  
  * **발동 조건:** NS가 50(Normal) 미만으로 떨어지는 순간 **전투당 1회** 자동 발동.  
  * **기믹:** 성공 확률 5% \+ QTE 입력.  
  * **성공 시 (Overclock):** NS 수치가 즉시 \*\*160(Inspired)\*\*으로 회복되며 위기를 기회로 전환.  
  * **실패 시 (ClockLock):** 시스템이 잠기며 즉시 **무력화(Incapacitated)** 상태로 전환. 회복 불가.  
    

## **13.2. 지휘관 시스템 (Commander System)**

* 개요: 일부 적 유닛은 '지휘관' 속성을 가지며, 전장 내 다른 일반 유닛들의 행동 패턴에 영향을 준다  
* 효과 :   
  AI 강화 \- 엘리트/보스 유닛은 본인의 AI 수준과 별개로 주변 아군에게 `CommandAIBonus`를 부여하여 유동적인 AI 성능 향상을 유발함. (음수도 가능)

**13.2.2. AI 아키타입 (Archetypes)** \> 1\. **Aggressive (돌격형):** 근접 거리 점수 가중치 높음. 산탄총 효율 극대화 지점 선호.

2\. **Tactical (전술형):** 엄폐 및 각도 계수(AngleFactor)가 낮은 지점(측면) 선호.

3\. **Sniper (저격형):** 원거리 및 고지대(HeightFactor) 가중치 높음.

**13.2.3. 의사결정 방식:** 초기 단계에서는 Utility AI를 사용하여 행동별 점수를 산출하며, 추후 딥러닝을 통해 최적 가중치 산출 예정.

# **14.0.맵 에디터 시스템 (Map Editor System)**

본 챕터는 개발 생산성을 위해 Unity 에디터 상에서 동작하는 \*\*레벨 디자인 툴(Level Design Tool)\*\*의 사양을 정의한다.

### **14.1. 개발 범위 및 편집 단위 (Scope)**

* **목표:** XCOM 2 수준의 전술 맵 제작을 위한 **최소 기능 단위(MVP)** 구현.  
* **필수 편집 대상:**  
  1. **Tile (Floor):** 유닛이 밟는 바닥.  
  2. **Pillar (Column):** Tile 위에 올라가는 오브젝트.  
  3. **Edge (Wall/Window/Door):** **\[핵심\]** 엄폐(Cover) 및 이동 경로를 결정하는 벽면 데이터.  
* **제외 대상 (Phase 2):** 복잡한 컷신 트리거, 적 AI 순찰 경로 디테일 설정 등은 추후 구현한다.

### **14.2. 편집 워크플로우 (Workflow: Scene-First)**

데이터를 직접 수정하는 것이 아니라, \*\*'눈에 보이는 객체'\*\*를 조작하고 \*\*'데이터'\*\*로 굽는 방식을 채택한다.

1. **배치 (Place):** 팔레트에서 타일/벽을 선택하여 씬(Scene)에 `EditorTile` 프리팹을 생성.  
2. **조작 (Manipulate):** Unity 기본 툴(Move/Rotate) 및 `Undo/Redo` 기능을 그대로 사용.  
3. **검증 (Validate):** '유효성 검사' 버튼을 통해 맵 밖으로 나간 타일이나 겹친 벽을 감지.  
4. **저장 (Bake):** 저장 버튼 클릭 시, 씬의 모든 `EditorTile` 정보를 긁어모아 `MapDataSO`로 변환(Overwrite).  
5. 편집 논리 (Editor Logic) :   
   1. 타일 우선 원칙: 바닥 타일이 없는 좌표에는 벽/기둥 배치 불가.   
   2. 자동 동기화: 기둥 배치 시 해당 좌표 \`EditorTile\`의 \`PillarID\`를 즉시 갱신.

### **14.3. UX 및 조작 (Interaction)**

* **층별 격리 (Layer Slicing):**  
  * 현재 편집 중인 층(Y Level)을 제외한 다른 층은 **반투명(Ghost)** 처리하거나 숨김.  
  * 마우스 클릭 레이캐스트는 오직 **현재 층의 그리드**에만 반응 (오클릭 방지).  
* **그리드 스냅 (Grid Snap):**  
  * 모든 객체는 `GridUtils`에 정의된 좌표계(`1.0m` 간격, `2.5m` 높이)에 자동으로 스냅됨.  
* **툴 모드 (Tool Modes):**  
  * `Draw Mode`: 마우스 드래그로 바닥을 붓칠하듯 배치.  
  * `Object Mode`: 벽, 기둥, 스폰 포인트를 개별 클릭으로 배치 및 회전.

### **14.4. 코드 및 데이터 정책**

* **공유 로직:** 좌표 변환(`GridUtils`) 및 저장 구조체(`MapDataStructures`)는 런타임과 공유한다.  
* **분리 로직:** 맵 로딩(`MapManager`) 등의 무거운 런타임 코드는 에디터에서 실행하지 않으며, 에디터 전용의 경량화된 로더를 사용한다.  
* **참조 방식:** 에디터는 프리팹을 직접 저장하지 않고, `MapEditorSettingsSO`에 정의된 **Enum ID(String Key)** 만을 저장하여 데이터 무결성을 유지한다.

