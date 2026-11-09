import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { AtexStatusDisplay } from 'components/Displays/RobotDisplays/AtexStatusDisplay'
import { RobotStatus, RobotType, placeholderRobot } from 'models/Robot'
import { getAtexPressureStatus } from 'utils/atexPressure'
import { DashboardRobotInfo } from 'pages/DashboardPage/DashboardRobotInfo'
import { RobotCard } from 'pages/FrontPage/MissionOverview/RobotCard'
import { MemoryRouter } from 'react-router'
import { InstallationContext } from 'contexts/InstallationContext'

vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))

vi.mock('hooks/useRobotTelemetry', () => ({
    useRobotTelemetry: () => ({ robotPressureLevel: 0.2, robotBatteryLevel: 82 }),
}))

const robot = {
    ...placeholderRobot,
    isAtex: true,
    lowerPressureWarningThreshold: 0.1,
    upperPressureWarningThreshold: 0.3,
}

describe('ATEX pressure classification', () => {
    test.each([
        [0.0999, 'low'],
        [0.1, 'normal'],
        [0.2, 'normal'],
        [0.3, 'normal'],
        [0.3001, 'high'],
        [undefined, 'unknown'],
        [NaN, 'unknown'],
        [Infinity, 'unknown'],
    ] as const)('classifies unrounded pressure %s as %s', (pressure, expected) => {
        expect(getAtexPressureStatus(robot, pressure)).toBe(expected)
    })

    test('uses the limits of each individual robot', () => {
        expect(getAtexPressureStatus(robot, 0.2)).toBe('normal')
        expect(getAtexPressureStatus({ ...robot, lowerPressureWarningThreshold: 0.25 }, 0.2)).toBe('low')
    })

    test.each([
        { lowerPressureWarningThreshold: null },
        { upperPressureWarningThreshold: undefined },
        { lowerPressureWarningThreshold: NaN },
        { upperPressureWarningThreshold: Infinity },
        { lowerPressureWarningThreshold: 0.3 },
        { lowerPressureWarningThreshold: 0.4 },
    ])('does not show normal with incomplete/invalid configuration %s', (limits) => {
        expect(getAtexPressureStatus({ ...robot, ...limits }, 0.2)).toBe('unconfigured')
    })

    test('never reports an offline or disconnected robot as within limits', () => {
        expect(getAtexPressureStatus({ ...robot, status: RobotStatus.Offline }, 0.2)).toBe('offline')
        expect(getAtexPressureStatus({ ...robot, status: RobotStatus.ConnectionIssues }, 0.2)).toBe('unknown')
        expect(getAtexPressureStatus({ ...robot, status: RobotStatus.UnknownStatus }, 0.2)).toBe('unknown')
    })
})

describe('ATEX pressure display', () => {
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

    test.each([RobotType.AnymalD, RobotType.AnymalX, RobotType.Robot])(
        'does not show an ATEX badge for non-ATEX %s, even with pressure telemetry',
        (type) => {
            act(() => root.render(<AtexStatusDisplay robot={{ ...robot, type, isAtex: false }} pressure={0.2} />))
            expect(container.textContent).toBe('')
        }
    )

    test('shows text as well as a value, and remains visible when pressure expires or the robot goes offline', () => {
        act(() => root.render(<AtexStatusDisplay robot={robot} pressure={0.05} />))
        expect(container.textContent).toContain('Pressure too low')
        expect(container.textContent).toContain('50 mbar')

        act(() => root.render(<AtexStatusDisplay robot={robot} />))
        expect(container.textContent).toContain('Unknown - no fresh pressure')
        expect(container.textContent).not.toContain('50 mbar')

        act(() => root.render(<AtexStatusDisplay robot={{ ...robot, status: RobotStatus.Offline }} pressure={0.2} />))
        expect(container.textContent).toContain('Unknown - robot offline')
        expect(container.querySelector('[role="status"]')).not.toBeNull()
    })

    test.each([DashboardRobotInfo, RobotCard])('keeps the ATEX indicator visible on offline robot cards', (Card) => {
        act(() =>
            root.render(
                <MemoryRouter>
                    <InstallationContext.Provider value={{ installation: robot.currentInstallation }}>
                        <Card robot={{ ...robot, status: RobotStatus.Offline }} />
                    </InstallationContext.Provider>
                </MemoryRouter>
            )
        )
        expect(container.textContent).toContain('ATEX pressure')
        expect(container.textContent).toContain('Unknown - robot offline')
        expect(container.textContent).not.toContain('Within limits')
    })
})
