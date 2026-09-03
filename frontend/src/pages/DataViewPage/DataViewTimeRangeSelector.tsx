import { Button, TextField } from '@equinor/eds-core-react'
import { ChangeEvent, useState } from 'react'
import { useLanguageContext } from 'contexts/LanguageContext'
import {
    CustomTimeRangeError,
    CustomTimeRangeField,
    CustomTimeRangeForm,
    TimeRangeToggle,
    TimeRangeToggleButton,
} from './DataViewComponents'
import {
    createCustomTimeRange,
    createPresetTimeRange,
    CustomDateRangeFailure,
    DataViewTimeRange,
    PresetDays,
    timeRangePresets,
    TimeRangeMode,
} from './DataViewTimeRange'

interface DataViewTimeRangeSelectorProps {
    ariaLabel: string
    activeMode: TimeRangeMode
    activeRange: DataViewTimeRange
    onApplyRange: (mode: TimeRangeMode, range: DataViewTimeRange) => void
}

const errorMessages = {
    'missing-date': 'Select both a start date and an end date',
    'invalid-range': 'Start date must not be after end date',
    'future-date': 'End date must not be in the future',
} as const

const formatDateInputValue = (date: Date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
}

export const DataViewTimeRangeSelector = ({
    ariaLabel,
    activeMode,
    activeRange,
    onApplyRange,
}: DataViewTimeRangeSelectorProps) => {
    const { TranslateText } = useLanguageContext()
    const [isCustomRangeOpen, setIsCustomRangeOpen] = useState(activeMode === 'custom')
    const [startDate, setStartDate] = useState(() => formatDateInputValue(activeRange.minDate))
    const [endDate, setEndDate] = useState(() => formatDateInputValue(activeRange.maxDate ?? new Date()))
    const [failure, setFailure] = useState<CustomDateRangeFailure | undefined>(undefined)

    const today = formatDateInputValue(new Date())

    const applyPreset = (days: PresetDays) => {
        setIsCustomRangeOpen(false)
        setFailure(undefined)
        onApplyRange(days, createPresetTimeRange(days))
    }

    const applyCustomRange = () => {
        const result = createCustomTimeRange(startDate, endDate)
        if (result.failure) {
            setFailure(result.failure)
            return
        }

        setFailure(undefined)
        onApplyRange('custom', result.range)
    }

    return (
        <>
            <TimeRangeToggle role="group" aria-label={ariaLabel}>
                {timeRangePresets.map(({ days, label }) => (
                    <TimeRangeToggleButton
                        key={days}
                        type="button"
                        variant={activeMode === days ? 'contained' : 'ghost'}
                        aria-pressed={activeMode === days}
                        onClick={() => applyPreset(days)}
                    >
                        {TranslateText(label)}
                    </TimeRangeToggleButton>
                ))}
                <TimeRangeToggleButton
                    type="button"
                    variant={activeMode === 'custom' ? 'contained' : 'ghost'}
                    aria-pressed={activeMode === 'custom'}
                    aria-expanded={isCustomRangeOpen}
                    aria-controls="data-view-custom-range"
                    onClick={() => {
                        if (isCustomRangeOpen) {
                            setIsCustomRangeOpen(false)
                            return
                        }
                        setStartDate(formatDateInputValue(activeRange.minDate))
                        setEndDate(formatDateInputValue(activeRange.maxDate ?? new Date()))
                        setIsCustomRangeOpen(true)
                        setFailure(undefined)
                    }}
                >
                    {TranslateText('Custom')}
                </TimeRangeToggleButton>
            </TimeRangeToggle>
            {isCustomRangeOpen && (
                <div id="data-view-custom-range">
                    <CustomTimeRangeForm>
                        <CustomTimeRangeField>
                            <TextField
                                id="data-view-start-date"
                                type="date"
                                label={TranslateText('Start date')}
                                value={startDate}
                                max={endDate || today}
                                variant={failure?.invalidFields.includes('start') ? 'error' : undefined}
                                onChange={(event: ChangeEvent<HTMLInputElement>) => {
                                    setStartDate(event.target.value)
                                    setFailure(undefined)
                                }}
                            />
                        </CustomTimeRangeField>
                        <CustomTimeRangeField>
                            <TextField
                                id="data-view-end-date"
                                type="date"
                                label={TranslateText('End date')}
                                value={endDate}
                                min={startDate || undefined}
                                max={today}
                                variant={failure?.invalidFields.includes('end') ? 'error' : undefined}
                                onChange={(event: ChangeEvent<HTMLInputElement>) => {
                                    setEndDate(event.target.value)
                                    setFailure(undefined)
                                }}
                            />
                        </CustomTimeRangeField>
                        <Button type="button" onClick={applyCustomRange}>
                            {TranslateText('Apply')}
                        </Button>
                    </CustomTimeRangeForm>
                    <CustomTimeRangeError role="alert">
                        {failure ? TranslateText(errorMessages[failure.error]) : ''}
                    </CustomTimeRangeError>
                </div>
            )}
        </>
    )
}
