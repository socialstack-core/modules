import Input from 'UI/Input';

export interface AttributeUnitsProps {
	name?: string,
	value?: string,
	label?: string,
	defaultValue?: string
}

export default function AttributeUnits(props: AttributeUnitsProps) {

	const unitGroupSet = [
		{
			name: `Length/ Distance`,
			values: [
				{
					name: `Millimeters`,
					value: 'mm'
				},
				{
					name: `Centimeters`,
					value: 'cm'
				},
				{
					name: `Meters`,
					value: 'm'
				},
				{
					name: `Kilometers`,
					value: 'km'
				},
				{
					name: `Micrometers`,
					value: 'µm'
				},
				{
					name: `Nanometers`,
					value: 'nm'
				}
			]
		},
		{
			name: `Area`,
			values: [
				{
					name: `Square Millimeters`,
					value: 'mm²'
				},
				{
					name: `Square Centimeters`,
					value: 'cm²'
				},
				{
					name: `Square Meters`,
					value: 'm²'
				},
				{
					name: `Square Kilometers`,
					value: 'km²'
				}
			]
		},
		{
			name: `Volume`,
			values: [
				{
					name: `Milliliters`,
					value: 'ml'
				},
				{
					name: `Liters`,
					value: 'L'
				},
				{
					name: `Cubic centimeters`,
					value: 'cm³'
				},
				{
					name: `Cubic meters`,
					value: 'm³'
				},
				{
					name: `Microliters`,
					value: 'µl'
				}
			]
		},
		{
			name: `Mass/ weight`,
			values: [
				{
					name: `Milligrams`,
					value: 'mg'
				},
				{
					name: `Grams`,
					value: 'g'
				},
				{
					name: `Kilograms`,
					value: 'kg'
				},
				{
					name: `Metric tons (1,000 kg)`,
					value: 'tonne'
				},
				{
					name: `Micrograms`,
					value: 'µg'
				}
			]
		},
		{
			name: `Temperature`,
			values: [
				{
					name: `Celsius`,
					value: '°C'
				},
				{
					name: `Kelvin`,
					value: 'K'
				}
			]
		},
		{
			name: `Energy/ Power`,
			values: [
				{
					name: `Joules`,
					value: 'J'
				},
				{
					name: `kilojoule`,
					value: 'kJ'
				},
				{
					name: `watt-hour`,
					value: 'Wh'
				},
				{
					name: `kilowatt-hour`,
					value: 'kWh'
				},
				{
					name: `Watts`,
					value: 'W'
				},
				{
					name: `Kilowatts`,
					value: 'kW'
				},
			]
		},
		{
			name: `Chemical/ Concentration`,
			values: [
				{
					name: `Moles`,
					value: 'mol'
				},
				{
					name: `Millimole per liter`,
					value: 'mmol/L'
				},
				{
					name: `Grams per liter`,
					value: 'g/L'
				},
				{
					name: `Milligrams per milliliter`,
					value: 'mg/ml'
				},
				{
					name: `Percent`,
					value: '%'
				},
				{
					name: `Parts per million`,
					value: 'ppm'
				},
				{
					name: `Parts per billion`,
					value: 'ppb'
				},
				{
					name: `Acidity/ basicity`,
					value: 'pH'
				},
				{
					name: `Becquerels (radioactivity)`,
					value: 'Bq'
				}
			]
		},
		{
			name: `Electrical`,
			values: [
				{
					name: `Volts`,
					value: 'V'
				},
				{
					name: `Amperes`,
					value: 'A'
				},
				{
					name: `Milliamperes`,
					value: 'mA'
				},
				{
					name: `Ohms`,
					value: 'Ω'
				},
				{
					name: `Hertz`,
					value: 'Hz'
				},
			]
		},
		{
			name: `Time`,
			values: [
				{
					name: `Milliseconds`,
					value: 'ms'
				},
				{
					name: `Seconds`,
					value: 's'
				},
				{
					name: `Minutes`,
					value: 'minutes'
				},
				{
					name: `Hours`,
					value: 'hours'
				},
				{
					name: `Days`,
					value: 'days'
				},
				{
					name: `Months`,
					value: 'months'
				},
				{
					name: `Years`,
					value: 'years'
				},
			]
		},
		{
			name: `Geometry`,
			values: [
				{
					name: `Degrees (angle)`,
					value: '°'
				},
				{
					name: `Radians`,
					value: 'rad'
				},
			]
		},
		{
			name: `Pressure/ Force`,
			values: [
				{
					name: `Pascals`,
					value: 'Pa'
				},
				{
					name: `Bars`,
					value: 'bar'
				},
				{
					name: `Newtons`,
					value: 'N'
				},
				{
					name: `Kilonewtons`,
					value: 'kN'
				},
			]
		},
		{
			name: `Lighting`,
			values: [
				{
					name: `Illuminance`,
					value: 'lux'
				},
				{
					name: `Candela (luminous intensity)`,
					value: 'cd'
				},
				{
					name: `Lumen (luminous flux)`,
					value: 'lumens'
				}
			]
		},
	];

	return <Input {...props} type='select'>
		<option value=''>Please select</option>
		{
			unitGroupSet.map(groupInfo => {
				return <optgroup label={ groupInfo.name} >
					{groupInfo.values.map(unitInfo => <option value={unitInfo.value}>{unitInfo.name + ' - ' + unitInfo.value}</option>)}
				</optgroup>
			})
		}
	</Input>;

}
