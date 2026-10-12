import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { MissionDefinition } from 'models/MissionDefinition'
import { LanguageProvider } from 'contexts/LanguageContext'
import { MissionSchedulingTable } from 'pages/MissionSchedulingComponents/MissionSchedulingTable'

vi.mock('react-router', () => ({ useNavigate: () => vi.fn() }))
vi.mock('contexts/AssetContext', () => ({
    useAssetContext: () => ({ enabledRobots: [] }),
}))
vi.mock('contexts/MissionRunsContext', () => ({
    useMissionsContext: () => ({ ongoingMissions: [], missionQueue: [] }),
}))
vi.mock('contexts/InstallationContext', async () => {
    const { createContext } = await import('react')
    return { InstallationContext: createContext({ installation: { installationCode: 'test' } }) }
})

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    window.localStorage.setItem('flotilla-language', 'en')
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
    window.localStorage.clear()
    vi.unstubAllGlobals()
})

const renderMission = (analysisTypesPerTask: string[][]) => {
    // Use serialized API values rather than the frontend's SARA enum values.
    const mission: MissionDefinition = JSON.parse(
        JSON.stringify({
            id: 'mission-1',
            name: 'Oil inspection',
            isAdHoc: false,
            installationCode: 'test',
            inspectionArea: { id: 'area-1', inspectionAreaName: 'Test area' },
            tasks: analysisTypesPerTask.map((analysisTypes) => ({ analysisTypes })),
        })
    )

    act(() => {
        root.render(
            <LanguageProvider>
                <MissionSchedulingTable
                    inspectionArea={mission.inspectionArea}
                    missionDefinitions={[mission]}
                    setSelectedMissions={vi.fn()}
                />
            </LanguageProvider>
        )
    })

    return container.querySelector('tbody')?.querySelector('[id="Analysis type"]')
}

test('displays every serialized Flotilla analysis type and removes duplicates across tasks', () => {
    const cell = renderMission([
        ['CLOE', 'Fencilla'],
        ['ThermalReading', 'CO2', 'CLOE'],
    ])

    expect(cell?.textContent).toBe(
        'Constant level oiler, Perimeter breach detection, Thermal reading, CO2 concentration'
    )
})

test('displays the requested Norwegian oil level label', () => {
    window.localStorage.setItem('flotilla-language', 'no')

    expect(renderMission([['CLOE']])?.textContent).toBe('Nivåmåling av oljeglass')
})

test.each([{ tasks: [] }, { tasks: [[]] }])('displays None when no analyses are configured: %j', ({ tasks }) => {
    expect(renderMission(tasks)?.textContent).toBe('None')
})

test('keeps an unrecognized API analysis type visible instead of rendering a blank', () => {
    expect(renderMission([['FutureAnalysis']])?.textContent).toBe('FutureAnalysis')
})
