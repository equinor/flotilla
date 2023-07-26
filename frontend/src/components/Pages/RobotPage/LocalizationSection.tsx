import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { LocalizationDialog } from './LocalizationDialog'

interface RobotProps {
    robot: Robot
}

export function LocalizationSection({ robot }: RobotProps) {
    const { TranslateText } = useLanguageContext()
    return (
        <>
            <Typography variant="h2">{TranslateText('Localization')}</Typography>
            <LocalizationDialog robot={robot} />
        </>
    )
}
