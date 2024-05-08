
use crate::calculations;



pub fn test_calculation() {

    let arr = [[1.0, 1.0, 1.0, 1.0], [3.0, 3.0, 3.0, 3.0], [1.0, 1.0, 1.0, 1.0], [3.0, 3.0, 3.0, 3.0]];
    let x = 0.5;
    let y = 0.5;
    let result = calculations::bicubic_interpolation(&arr, x, y);
    assert!(result.unwrap() == 2.0);
}