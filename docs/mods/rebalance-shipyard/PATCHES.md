# Rebalance Shipyard — 훅 목록

## 0.1.0
| 대상 | 종류 | RVA (GameAssembly 50D53D17…) | 동작 |
|---|---|---|---|
| `Client.PlayerStore.WorldPortBuildShipData.GetBuyShipList(int[])` | Postfix | 0xB71E40 | 반환 List<int> 에서 `Ship.technical > 0` 제거, 비면 유지 |
