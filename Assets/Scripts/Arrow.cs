using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform arrowHead;

    [SerializeField] private float headLength = 0.37f;

    public void DrawArrow(Vector3[] worldPositions)
    {
        // 1. 예외 처리
        if (worldPositions == null || worldPositions.Length < 2) return;

        // 2. 화살표 머리 각도 정하기
        Vector3 endPos = worldPositions[worldPositions.Length - 1]; // 끝점
        Vector3 endPrePos = worldPositions[worldPositions.Length - 2]; // 끝점 직전 지점

        Vector3 direction = (endPos - endPrePos).normalized;

        this.arrowHead.position = endPos;

        // 화살표 방향 회전 값 계산
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        this.arrowHead.rotation = Quaternion.Euler(0, 0, angle - 90.0f);

        Vector3 lineEndPos = endPos - (direction * this.headLength);
        // 2. 화살표 선 그리기
        this.lineRenderer.positionCount = worldPositions.Length;
        for (int index = 0; index < worldPositions.Length - 1; index++)
        {
            this.lineRenderer.SetPosition(index, worldPositions[index]);
        }

        this.lineRenderer.SetPosition(worldPositions.Length - 1, lineEndPos);
    }

    public void Clear()
    {
        this.lineRenderer.positionCount = 0;
        gameObject.SetActive(false);
    }
}
