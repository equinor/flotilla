import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { PressureStatusDisplay } from 'components/Displays/RobotDisplays/PressureStatusDisplay'
import { InstallationContext } from 'contexts/InstallationContext'
import { RobotStatus, RobotType, placeholderRobot } from 'models/Robot'
import { DashboardRobotInfo } from 'pages/DashboardPage/DashboardRobotInfo'
import { RobotCard } from 'pages/FrontPage/MissionOverview/RobotCard'

vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))

vi.mock('hooks/useRobotTelemetry', () => ({
    useRobotTelemetry: () => ({ robotPressureLevel: 0.2, robotBatteryLevel: 82 }),
}))

let container: HTMLDivElement
let root: Root
const taurob = { ...placeholderRobot, type: RobotType.TaurobInspector }

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

describe.each([RobotType.TaurobInspector, RobotType.TaurobOperator])('%s ATEX indication', (type) => {
    test.each(Object.values(RobotStatus).filter((status) => status !== RobotStatus.Offline))(
        'shows ATEX active for %s without requiring pressure telemetry',
        (status) => {
            act(() => root.render(<PressureStatusDisplay robot={{ ...taurob, type, status }} />))
            expect(container.querySelector('[aria-label="ATEX"]')?.textContent).toBe('Active')
            expect(container.textContent).not.toContain('mBar')
        }
    )

    test('shows unknown while offline and returns to active when available', () => {
        act(() =>
            root.render(
                <PressureStatusDisplay robot={{ ...taurob, type, status: RobotStatus.Offline }} pressure={0.2} />
            )
        )
        expect(container.textContent).toContain('Unknown - robot offline')
        expect(container.textContent).not.toContain('Active')
        act(() => root.render(<PressureStatusDisplay robot={{ ...taurob, type }} />))
        expect(container.querySelector('[aria-label="ATEX"]')?.textContent).toBe('Active')
    })

    test.each([0, 0.2, 100, undefined])('does not use pressure %s to determine ATEX status', (pressure) => {
        act(() => root.render(<PressureStatusDisplay robot={{ ...taurob, type }} pressure={pressure} />))
        expect(container.querySelector('[aria-label="ATEX"]')?.textContent).toBe('Active')
    })
})

test.each([RobotType.AnymalD, RobotType.AnymalX])('never shows pressure or ATEX for %s', (type) => {
    act(() => root.render(<PressureStatusDisplay robot={{ ...placeholderRobot, type }} pressure={0.2} />))
    expect(container.textContent).toBe('')
})

test('preserves ordinary pressure for the mock robot without showing ATEX', () => {
    act(() => root.render(<PressureStatusDisplay robot={placeholderRobot} pressure={0.2} />))
    expect(container.textContent).toContain('200mBar')
    expect(container.textContent).not.toContain('ATEX')
    act(() => root.render(<PressureStatusDisplay robot={placeholderRobot} />))
    expect(container.textContent).toBe('')
    act(() =>
        root.render(
            <PressureStatusDisplay robot={{ ...placeholderRobot, status: RobotStatus.Offline }} pressure={0.2} />
        )
    )
    expect(container.textContent).toBe('')
})

describe.each([DashboardRobotInfo, RobotCard])('robot card integration', (Card) => {
    test('keeps the ATEX indicator visible for offline Taurob', () => {
        act(() =>
            root.render(
                <MemoryRouter>
                    <InstallationContext.Provider value={{ installation: taurob.currentInstallation }}>
                        <Card robot={{ ...taurob, status: RobotStatus.Offline }} />
                    </InstallationContext.Provider>
                </MemoryRouter>
            )
        )
        expect(container.querySelector('[aria-label="ATEX"]')?.textContent).toBe('Unknown - robot offline')
    })

    test.each([RobotType.AnymalD, RobotType.AnymalX])('hides pressure and ATEX for %s even with telemetry', (type) => {
        act(() =>
            root.render(
                <MemoryRouter>
                    <InstallationContext.Provider value={{ installation: taurob.currentInstallation }}>
                        <Card robot={{ ...placeholderRobot, type }} />
                    </InstallationContext.Provider>
                </MemoryRouter>
            )
        )
        expect(container.textContent).not.toContain('Pressure')
        expect(container.textContent).not.toContain('ATEX')
        expect(container.textContent).not.toContain('mBar')
    })
})
