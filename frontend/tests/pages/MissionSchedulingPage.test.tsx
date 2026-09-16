import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionSchedulingPage } from 'pages/MissionSchedulingPage'

const state = vi.hoisted((): { missionDefinitions: MissionDefinition[]; inspectionAreas: InspectionArea[] } => ({
    missionDefinitions: [],
    inspectionAreas: [],
}))

vi.mock('contexts/InstallationContext', async () => {
    const { createContext } = await import('react')
    return { InstallationContext: createContext({ installation: { installationCode: 'test' } }) }
})
vi.mock('contexts/LanguageContext', () => ({
    useLanguageContext: () => ({ TranslateText: (text: string) => text }),
}))
vi.mock('contexts/MissionRunsContext', () => ({
    useMissionsContext: () => ({ ongoingMissions: [], missionQueue: [] }),
}))
vi.mock('contexts/AssetContext', () => ({
    useAssetContext: () => ({ installationInspectionAreas: state.inspectionAreas }),
}))
vi.mock('contexts/MissionDefinitionsContext', () => ({
    useMissionDefinitionsContext: () => ({ missionDefinitions: state.missionDefinitions }),
}))
vi.mock('components/Header/Header', () => ({ Header: () => null }))
vi.mock('components/Header/NavBar', () => ({ NavBar: () => null }))
vi.mock('pages/MissionPage/MapPosition/PointillaMapView', () => ({ PlantPolygonMap: () => null }))
vi.mock('pages/MissionSchedulingComponents/InspectionAreaCards', () => ({
    InspectionAreaCard: ({
        inspectionArea,
        nMissions,
        onClickInspectionArea,
        onClickScheduleAll,
    }: {
        inspectionArea: InspectionArea
        nMissions: number
        onClickInspectionArea: (area: InspectionArea) => void
        onClickScheduleAll: (area: InspectionArea) => void
    }) => (
        <div data-area={inspectionArea.id}>
            <span data-count>{nMissions}</span>
            <button data-select onClick={() => onClickInspectionArea(inspectionArea)}>
                Select area
            </button>
            <button data-schedule onClick={() => onClickScheduleAll(inspectionArea)}>
                Schedule all
            </button>
        </div>
    ),
}))
vi.mock('pages/MissionSchedulingComponents/MissionSchedulingTable', () => ({
    MissionSchedulingTable: ({ missionDefinitions }: { missionDefinitions: MissionDefinition[] }) => (
        <div data-table>{missionDefinitions.map((mission) => mission.id).join(',')}</div>
    ),
}))
vi.mock('pages/MissionSchedulingComponents/ScheduleMissionDialogs', () => ({
    ScheduleMissionDialog: ({ selectedMissions }: { selectedMissions: MissionDefinition[] }) => (
        <div data-selected>{selectedMissions.map((mission) => mission.id).join(',')}</div>
    ),
}))

const area: InspectionArea = {
    id: 'area-1',
    inspectionAreaName: 'Area 1',
    plantName: 'Test plant',
    plantCode: 'test',
    installationCode: 'test',
}
const plannedMission: MissionDefinition = {
    id: 'planned-1',
    name: 'Planned mission',
    installationCode: 'test',
    inspectionArea: area,
    isAdHoc: false,
    tasks: [],
}
const adHocMission: MissionDefinition = {
    ...plannedMission,
    id: 'ad-hoc-1',
    name: 'Ad hoc mission',
    isAdHoc: true,
}

let container: HTMLDivElement
let root: Root

beforeEach(() => {
    vi.stubGlobal('IS_REACT_ACT_ENVIRONMENT', true)
    state.inspectionAreas = [area, { ...area, id: 'area-2' }]
    state.missionDefinitions = [plannedMission, adHocMission]
    container = document.createElement('div')
    document.body.appendChild(container)
    root = createRoot(container)
})

afterEach(() => {
    act(() => root.unmount())
    container.remove()
    vi.unstubAllGlobals()
})

test('excludes ad hoc missions from area counts, the table, and Schedule all after context updates', () => {
    act(() => root.render(<MissionSchedulingPage />))
    expect(container.querySelector('[data-area="area-1"] [data-count]')?.textContent).toBe('1')
    expect(container.querySelector('[data-area="area-2"] [data-count]')?.textContent).toBe('0')

    act(() => container.querySelector<HTMLButtonElement>('[data-area="area-1"] [data-select]')!.click())
    expect(container.querySelector('[data-table]')?.textContent).toBe('planned-1')

    state.missionDefinitions = [
        ...state.missionDefinitions,
        { ...adHocMission, id: 'ad-hoc-2' },
        { ...plannedMission, id: 'planned-2', name: 'Second planned mission' },
    ]
    act(() => root.render(<MissionSchedulingPage />))
    expect(container.querySelector('[data-area="area-1"] [data-count]')?.textContent).toBe('2')
    expect(container.querySelector('[data-table]')?.textContent).toBe('planned-1,planned-2')

    act(() => container.querySelector<HTMLButtonElement>('[data-area="area-1"] [data-schedule]')!.click())
    expect(container.querySelector('[data-selected]')?.textContent).toBe('planned-1,planned-2')
    expect(state.missionDefinitions).toContain(adHocMission)
})

test('shows the empty state when the only inspection area contains only ad hoc missions', () => {
    state.inspectionAreas = [area]
    state.missionDefinitions = [adHocMission]

    act(() => root.render(<MissionSchedulingPage />))

    expect(container.textContent).toContain('No missions defined in this area')
    expect(container.querySelector('[data-table]')).toBeNull()
    expect(container.querySelector('[data-selected]')).toBeNull()
})
