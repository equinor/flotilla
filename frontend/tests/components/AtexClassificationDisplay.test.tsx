import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { AtexClassificationDisplay } from 'components/Displays/RobotDisplays/AtexClassificationDisplay'
import { RobotStatus, RobotType, placeholderRobot, robotAtexClassification } from 'models/Robot'
import { DashboardRobotInfo } from 'pages/DashboardPage/DashboardRobotInfo'

vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))

vi.mock('hooks/useRobotTelemetry', () => ({
    useRobotTelemetry: () => ({ robotPressureLevel: 0.2, robotBatteryLevel: 82 }),
}))

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true })
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
})

test.each([
    [RobotType.TaurobInspector, 'ATEX'],
    [RobotType.TaurobOperator, 'ATEX'],
    [RobotType.AnymalD, 'Non-ATEX'],
    [RobotType.Robot, 'Non-ATEX'],
    [RobotType.AnymalX, null],
    [RobotType.Turtlebot, null],
    [RobotType.NoneType, null],
] as const)('renders the confirmed classification for %s, hiding unknowns', (type, label) => {
    act(() => root.render(<AtexClassificationDisplay classification={robotAtexClassification[type]} />))
    expect(container.querySelector('[role="status"]')?.textContent ?? null).toBe(label)
})

test('shows Taurob pressure separately and keeps only the classification when offline', () => {
    const robot = { ...placeholderRobot, type: RobotType.TaurobInspector }
    act(() => root.render(<DashboardRobotInfo robot={robot} />))
    expect(container.textContent).toContain('Pressure200mBar')
    expect(container.querySelector('[role="status"]')?.textContent).toBe('ATEX')
    act(() => root.render(<DashboardRobotInfo robot={{ ...robot, status: RobotStatus.Offline }} />))
    expect(container.textContent).not.toContain('mBar')
    expect(container.querySelector('[role="status"]')?.textContent).toBe('ATEX')
})
