import { act } from 'react'
import { createRoot, Root } from 'react-dom/client'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { MissionProgressDisplay } from 'components/Displays/MissionDisplays/MissionProgressDisplay'
import { LanguageProvider } from 'contexts/LanguageContext'
import { placeholderMission } from 'models/Mission'

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

test('shows the stored routine instead of misleading zero-task progress', () => {
    act(() => {
        root.render(
            <LanguageProvider>
                <MissionProgressDisplay mission={{ ...placeholderMission, robotMissionId: 'DiagnosticRoutine' }} />
            </LanguageProvider>
        )
    })

    expect(container.textContent).toContain('Robot-stored mission')
    expect(container.textContent).toContain('DiagnosticRoutine')
    expect(container.textContent).not.toContain('0/0')
})

test('keeps inspection-task progress for ordinary missions', () => {
    act(() => {
        root.render(
            <LanguageProvider>
                <MissionProgressDisplay mission={placeholderMission} />
            </LanguageProvider>
        )
    })

    expect(container.textContent).toContain('Completed Tasks')
    expect(container.textContent).toContain('0/0')
    expect(container.textContent).not.toContain('Robot-stored mission')
})
